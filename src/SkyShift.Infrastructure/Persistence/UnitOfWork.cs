using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Infrastructure.Persistence;

/// <summary>
/// Implements IUnitOfWork: saves changes and commits or rolls back the ambient transaction opened
/// by SkyShiftDbContext. It translates specific database failures into domain exceptions: duplicate
/// flight, catalog model or email (unique indexes), a referenced vehicle delete (FK 547), and
/// RowVersion or serialization conflicts (1205/1222, including EF Core's transient-failure wrapper)
/// on a Vehicle or VehicleAssignment. Every other exception is rethrown unchanged.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SkyShiftDbContext _context;

    public UnitOfWork(SkyShiftDbContext context)
    {
        _context = context;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _context.CommitTransactionAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateFlight(ex))
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw new DuplicateFlightException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation<Vehicle>(ex, "IX_Vehicles_CatalogItemId"))
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw new CatalogItemInUseException();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation<User>(ex, "IX_Users_Email"))
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            var user = (User)ex.Entries.First(e => e.Entity is User).Entity;
            throw new EmailAlreadyRegisteredException(user.Email);
        }
        catch (DbUpdateException ex) when (IsDeletedVehicleStillReferenced(ex))
        {
            // Defence in depth for vehicle deletion: a VehicleAssignment or Flight
            // referencing the vehicle (FK, ON DELETE RESTRICT) blocks the DELETE.
            await _context.RollbackTransactionAsync(cancellationToken);
            var vehicle = (Vehicle)ex.Entries.First(e => e.Entity is Vehicle).Entity;
            throw VehicleDeletionNotAllowedException.HasHistory(vehicle.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var vehicleEntry = ex.Entries.FirstOrDefault(e => e.Entity is Vehicle);
            if (vehicleEntry is null)
            {
                // A concurrency conflict outside Vehicle is not an
                // anticipated MVP scenario (no other entity carries a
                // concurrency token) -- surfaced as-is rather than invented.
                await _context.RollbackTransactionAsync(cancellationToken);
                throw;
            }

            var vehicle = (Vehicle)vehicleEntry.Entity;
            var currentStatus = vehicle.Status;
            try
            {
                var databaseValues = await vehicleEntry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues is not null)
                    currentStatus = databaseValues.GetValue<VehicleStatus>(nameof(Vehicle.Status));
            }
            catch
            {
                // Reading the current DB value is a best-effort diagnostic
                // only; if it also fails, still translate using the
                // in-memory status rather than letting this secondary
                // failure mask the real concurrency conflict or skip the
                // transaction cleanup below.
            }
            finally
            {
                await _context.RollbackTransactionAsync(cancellationToken);
            }

            throw new VehicleNotAvailableException(vehicle.Id, currentStatus);
        }
        catch (DbUpdateException ex) when (IsSelectVehicleSerializationConflict(ex))
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            var translated = TranslateSerializationConflict(ex);
            if (translated is null)
                throw;
            throw translated;
        }
        // EF Core's default SQL Server execution strategy treats deadlock/lock-timeout
        // as transient and wraps the DbUpdateException in an InvalidOperationException
        // ("transient failure ... EnableRetryOnFailure"). Only that exact shape is
        // unwrapped here; any other InvalidOperationException propagates unchanged.
        // No automatic retry: replaying would require re-running the whole use case.
        catch (InvalidOperationException ex) when (ex.InnerException is DbUpdateException inner && IsSelectVehicleSerializationConflict(inner))
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            var translated = TranslateSerializationConflict(inner);
            if (translated is null)
                throw;
            throw translated;
        }
        catch
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    // The losing side of a serialization conflict on a Vehicle or VehicleAssignment
    // is reported as the vehicle not being available (409). Null when the conflict
    // involves neither entity -- that is not a failure this translation targets.
    private static Exception? TranslateSerializationConflict(DbUpdateException ex)
    {
        if (ex.Entries.FirstOrDefault(e => e.Entity is VehicleAssignment)?.Entity is VehicleAssignment assignment)
            return new VehicleNotAvailableException(assignment.VehicleId, VehicleStatus.InUse);

        if (ex.Entries.FirstOrDefault(e => e.Entity is Vehicle)?.Entity is Vehicle vehicle)
            return new VehicleNotAvailableException(vehicle.Id, VehicleStatus.InUse);

        return null;
    }

    private static bool IsDeletedVehicleStillReferenced(DbUpdateException ex)
    {
        if (!ex.Entries.Any(e => e.Entity is Vehicle && e.State == EntityState.Deleted))
            return false;

        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException sql && sql.Number == 547)
                return true;
        }

        return false;
    }

    /// <summary>
    /// True when the DbUpdateException's root cause is a SQL Server serialization conflict: 1205
    /// (deadlock victim) or 1222 (lock request timed out). Any other DbUpdateException is left
    /// untranslated by this filter.
    /// </summary>
    private static bool IsSelectVehicleSerializationConflict(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException sqlEx && (sqlEx.Number == 1205 || sqlEx.Number == 1222))
                return true;
        }

        return false;
    }
    /// <summary>
    /// True only for a SQL Server unique-key violation (2601/2627) on the
    /// named index, raised while saving an entity of type T. Any other
    /// failure is left untranslated.
    /// </summary>
    private static bool IsUniqueViolation<T>(DbUpdateException exception, string indexName)
    {
        if (!exception.Entries.Any(e => e.Entity is T))
            return false;

        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException sql && (sql.Number == 2601 || sql.Number == 2627)
                && sql.Message.Contains(indexName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsDuplicateFlight(DbUpdateException exception)
    {
        if (!exception.Entries.Any(e => e.Entity is Flight))
            return false;

        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is SqlException sql && (sql.Number == 2601 || sql.Number == 2627)
                && sql.Message.Contains("IX_Flights_UserId_Date", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
