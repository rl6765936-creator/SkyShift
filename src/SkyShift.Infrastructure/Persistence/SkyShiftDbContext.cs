using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for SkyShift. Also owns the small, Infrastructure-only
/// ambient-transaction mechanism (approved "Option A") that lets a read
/// (VehicleAssignmentRepository.GetActiveForUserAsync) and a later write in
/// the same scope (SelectVehicle's Vehicle update + VehicleAssignment
/// insert) share one Serializable-isolation transaction -- without
/// Application ever knowing a transaction exists.
///
/// How it works:
///   - EnsureTransactionAsync() is called only by the Repository methods
///     that actually participate in the SelectVehicle critical path:
///     VehicleAssignmentRepository.GetActiveForUserAsync/AddAsync and
///     VehicleRepository.GetByIdAsync/UpdateAsync. It is idempotent: only
///     the first call in a given DbContext scope actually opens a
///     transaction. Unrelated Repository methods (UserRepository,
///     FlightRepository, VehicleRepository.GetAvailableAsync) do not call
///     it and run as plain, untransacted reads/writes.
///   - UnitOfWork.SaveChangesAsync() calls CommitTransactionAsync() after a
///     successful SaveChanges, or RollbackTransactionAsync() on failure.
///   - Read-only Use Cases that call GetActiveForUserAsync or
///     Vehicle.GetByIdAsync (GetUserDashboard, GetUserVehicle) still open
///     this transaction on that call -- unavoidable, since those Use Cases
///     go through the exact same Repository methods SelectVehicle uses, and
///     Infrastructure has no way to tell the callers apart without a change
///     to Application. Nothing ever commits or rolls back that transaction
///     explicitly in those flows; it is released implicitly when the
///     DbContext/connection is disposed at the end of its DI scope (closing
///     a connection with an open transaction rolls it back).
/// </summary>
public class SkyShiftDbContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<User> Users => Set<User>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
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

        await transaction.RollbackAsync(cancellationToken);
        await transaction.DisposeAsync();
    }
}
