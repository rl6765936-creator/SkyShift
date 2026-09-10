using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Infrastructure.Persistence;

namespace SkyShift.Infrastructure.Repositories;

public class VehicleRepository : IVehicleRepository
{
    private readonly SkyShiftDbContext _context;

    public VehicleRepository(SkyShiftDbContext context)
    {
        _context = context;
    }

    // Part of the SelectVehicle critical path (loads the vehicle being
    // assigned) -- keeps the ambient Serializable transaction opened by
    // VehicleAssignmentRepository.GetActiveForUserAsync.
    public async Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await _context.EnsureTransactionAsync(cancellationToken);
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    // Not part of the SelectVehicle critical path -- a plain listing query,
    // no ambient transaction needed.
    public async Task<IReadOnlyList<Vehicle>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .Where(v => v.Status == VehicleStatus.Available)
            .ToListAsync(cancellationToken);
    }

    // Part of the SelectVehicle critical path -- the Vehicle.Assign() write.
    public async Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _context.EnsureTransactionAsync(cancellationToken);
        _context.Vehicles.Update(vehicle);
    }

    // Manager MVP addition. Not part of the SelectVehicle critical path --
    // Manager fleet management has no relation to the VehicleAssignment
    // concurrency race, so no ambient transaction is opened here.
    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
    }

    // Manager MVP addition. Plain listing query for Manager fleet
    // monitoring/Dashboard -- includes Retired vehicles (Manager must see
    // them, they are not hidden).
    public async Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    // Manager MVP addition. Deliberately separate from the existing
    // GetByIdAsync: that method opens the ambient SelectVehicle
    // Serializable transaction, which a Manager status-change read must
    // NOT join.
    public async Task<Vehicle?> GetByIdForManagementAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    // Manager MVP addition. Deliberately separate from the existing
    // UpdateAsync for the same reason as GetByIdForManagementAsync above --
    // no ambient transaction. DbSet.Update() is a synchronous, in-memory
    // change-tracker operation (no I/O), so there is nothing to await here
    // -- unlike UpdateAsync above, this method never calls
    // EnsureTransactionAsync. Not declared async (CS1998: an async method
    // with no await just runs synchronously anyway, with extra state-machine
    // overhead); returning Task.CompletedTask keeps the same Task-returning
    // signature IVehicleRepository/callers expect, with identical behavior.
    public Task UpdateStatusAsync(Vehicle vehicle, CancellationToken cancellationToken = default)
    {
        _context.Vehicles.Update(vehicle);
        return Task.CompletedTask;
    }
}
