using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Infrastructure.Persistence;

/// <summary>
/// Implements the Application-facing IUnitOfWork with exactly the one
/// approved member. Also the single place that commits or rolls back the
/// ambient Serializable transaction SkyShiftDbContext opened on first use in
/// this scope (see SkyShiftDbContext.EnsureTransactionAsync) -- Application
/// only ever sees SaveChangesAsync(); transaction demarcation is entirely
/// internal to Infrastructure.
///
/// Two concurrency failure modes are translated here, both to the existing
/// Domain VehicleNotAvailableException, so Application never needs to know
/// EF Core's or SQL Server's exception types:
///   - Problem 1 (two users selecting the same Vehicle at once): surfaces as
///     DbUpdateConcurrencyException from the Vehicle RowVersion token.
///   - Problem 2 (the same user's two concurrent SelectVehicle requests):
///     surfaces as a DbUpdateException wrapping a SQL Server serialization
///     conflict (deadlock/lock-timeout) on the VehicleAssignment insert --
///     the concrete failure mode of two Serializable transactions racing on
///     the same key range (see VehicleAssignmentRepository.GetActiveForUserAsync).
/// Only these specific, identified failures are translated; every other
/// exception -- including a DbUpdateException/DbUpdateConcurrencyException
/// that does not involve Vehicle/VehicleAssignment -- is rethrown unchanged.
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
            var assignmentEntry = ex.Entries.FirstOrDefault(e => e.Entity is VehicleAssignment);
            if (assignmentEntry is not null)
            {
                var assignment = (VehicleAssignment)assignmentEntry.Entity;
                await _context.RollbackTransactionAsync(cancellationToken);
                throw new VehicleNotAvailableException(assignment.VehicleId, VehicleStatus.InUse);
            }

            var vehicleEntry = ex.Entries.FirstOrDefault(e => e.Entity is Vehicle);
            if (vehicleEntry is not null)
            {
                var vehicle = (Vehicle)vehicleEntry.Entity;
                await _context.RollbackTransactionAsync(cancellationToken);
                throw new VehicleNotAvailableException(vehicle.Id, VehicleStatus.InUse);
            }

            // A serialization conflict that does not involve
            // VehicleAssignment or Vehicle is not the failure this
            // translation targets -- surfaced as-is.
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
        catch
        {
            await _context.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// True when the given DbUpdateException's root cause is a SQL Server
    /// serialization conflict: 1205 (deadlock victim) or 1222 (lock request
    /// timed out) -- the two concrete ways two Serializable transactions
    /// racing on the same key range can fail against each other. Any other
    /// DbUpdateException (a real constraint violation, a connectivity
    /// error, etc.) is left untranslated by this filter.
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
}
