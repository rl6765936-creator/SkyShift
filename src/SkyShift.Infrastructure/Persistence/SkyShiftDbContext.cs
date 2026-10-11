using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext that also owns an ambient transaction (Serializable on SQL Server, Read Committed
/// with explicit row locks on PostgreSQL) for the critical paths, vehicle selection and deletion,
/// without Application knowing about it. Only those repository methods open it, UnitOfWork commits or
/// rolls it back, and a transaction left open by a failing use case is rolled back when the DbContext
/// is disposed.
/// </summary>
public class SkyShiftDbContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleCatalogItem> VehicleCatalogItems => Set<VehicleCatalogItem>();
    public DbSet<VehicleAssignment> VehicleAssignments => Set<VehicleAssignment>();
    public DbSet<Flight> Flights => Set<Flight>();

    public SkyShiftDbContext(DbContextOptions<SkyShiftDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SkyShiftDbContext).Assembly);

        if (Database.IsNpgsql())
        {
            PostgreSqlModel.Apply(modelBuilder);
        }
        else
        {
            // SQL Server rowversion (also used by the InMemory tests); PostgreSQL uses xmin.
            modelBuilder.Entity<Vehicle>().Property<byte[]>("RowVersion").IsRowVersion();
        }
    }

    internal async Task EnsureTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            return;

        _currentTransaction = await Database.BeginTransactionAsync(CriticalPathIsolationLevel, cancellationToken);
    }

    // SQL Server: Serializable (range locks plus the UPDLOCK vehicle read). PostgreSQL: Read
    // Committed plus explicit row locks (FOR UPDATE on the vehicle, FOR NO KEY UPDATE on the
    // user). Under PostgreSQL's Serializable, a transaction that waited for a row lock fails
    // with 40001 instead of reading the winner's committed row, which would turn the expected
    // 404/409 outcomes into errors; at Read Committed the locked read returns the current row.
    private IsolationLevel CriticalPathIsolationLevel =>
        Database.IsNpgsql() ? IsolationLevel.ReadCommitted : IsolationLevel.Serializable;

    internal async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        var transaction = _currentTransaction;
        _currentTransaction = null;

        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();
    }

    internal async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
            return;

        var transaction = _currentTransaction;
        _currentTransaction = null;

        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch (InvalidOperationException) when (transaction.GetDbTransaction().Connection is null)
        {
            // SQL Server already rolled the transaction back (e.g. this session was
            // chosen as a deadlock victim), which leaves the client transaction
            // "completed" with no connection. Nothing remains to roll back; the
            // original failure is what the caller reports.
        }
        finally
        {
            await transaction.DisposeAsync();
        }
    }
}
