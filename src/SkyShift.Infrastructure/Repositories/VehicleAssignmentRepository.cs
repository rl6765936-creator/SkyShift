using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Infrastructure.Persistence;

namespace SkyShift.Infrastructure.Repositories;

public class VehicleAssignmentRepository : IVehicleAssignmentRepository
{
    private readonly SkyShiftDbContext _context;

    public VehicleAssignmentRepository(SkyShiftDbContext context)
    {
        _context = context;
    }

    public async Task<VehicleAssignment?> GetActiveForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        // Opens (or joins) the ambient Serializable transaction for this
        // scope BEFORE running the query, so that this read and the
        // eventual write in SelectVehicle are part of one atomic, isolated
        // unit -- see SkyShiftDbContext.EnsureTransactionAsync.
        await _context.EnsureTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Mirrors VehicleAssignment.IsActive's semantics exactly (inclusive
        // boundaries on both ends).
        return await _context.VehicleAssignments
            .AsNoTracking()
            .Where(VehicleAssignment.ActiveAt(now))
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
    }

    // Same query as GetActiveForUserAsync without joining the ambient transaction:
    // for dashboard/"my vehicle" display reads that never write.
    public async Task<VehicleAssignment?> GetActiveForUserForReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.VehicleAssignments
            .AsNoTracking()
            .Where(VehicleAssignment.ActiveAt(now))
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(VehicleAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _context.EnsureTransactionAsync(cancellationToken);
        await _context.VehicleAssignments.AddAsync(assignment, cancellationToken);
    }

    // Manager MVP addition. Not part of the SelectVehicle critical path --
    // Manager monitoring is read-only and unrelated to the concurrency
    // race, so no ambient transaction is opened here. Include()s the
    // User/Vehicle each assignment is between -- this listing IS the
    // many-to-many relationship's natural read path (a Manager viewing
    // "who currently has which vehicle"), so eager-loading both sides here
    // avoids an N+1 the moment either is needed, at the cost of one extra
    // JOIN on an already Manager-only, low-frequency query. The existing
    // AssignmentResponse mapping still only reads the scalar fields below --
    // Id/UserId/VehicleId/StartDate/EndDate/IsActive -- so this does not
    // change the endpoint's response shape.
    public async Task<IReadOnlyList<VehicleAssignment>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await _context.VehicleAssignments
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Vehicle)
            .Where(VehicleAssignment.ActiveAt(now))
            .ToListAsync(cancellationToken);
    }

    // Manager MVP addition. "Ended" mirrors GetAllActiveAsync's own
    // now-based IsActive check inverted (EndDate before now), so together
    // the two methods partition every VehicleAssignment row exactly once.
    public async Task<IReadOnlyList<VehicleAssignment>> GetHistoryAsync(int? userId, int? vehicleId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _context.VehicleAssignments
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Vehicle)
            .Where(VehicleAssignment.EndedBefore(now));

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (vehicleId.HasValue)
            query = query.Where(a => a.VehicleId == vehicleId.Value);

        return await query.ToListAsync(cancellationToken);
    }
}
