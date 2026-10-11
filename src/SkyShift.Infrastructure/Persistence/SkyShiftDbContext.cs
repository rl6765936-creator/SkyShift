using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext that also owns an ambient Serializable transaction, so reads and the following
/// writes of a critical path (vehicle selection, vehicle deletion) run in one transaction without
/// Application knowing about it. Only the repository methods on those paths call
/// EnsureTransactionAsync, which opens the transaction once per scope; UnitOfWork commits or rolls
/// it back, and display reads never open it. A transaction left open by a use case that throws
/// before saving is rolled back when the scope's DbContext is disposed.
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
    }

    internal async Task EnsureTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            return;

        _currentTransaction = await Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

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
