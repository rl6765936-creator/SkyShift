using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Infrastructure.Persistence;

namespace SkyShift.Infrastructure.Repositories;

public class FlightRepository : IFlightRepository
{
    private readonly SkyShiftDbContext _context;

    public FlightRepository(SkyShiftDbContext context)
    {
        _context = context;
    }

    // Not part of the SelectVehicle critical path -- Flight persistence is
    // unrelated to the VehicleAssignment concurrency race.
    public async Task AddAsync(Flight flight, CancellationToken cancellationToken = default)
    {
        await _context.Flights.AddAsync(flight, cancellationToken);
    }

    public async Task<int> GetCompletedCountAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Flights
            .AsNoTracking()
            .CountAsync(f => f.UserId == userId && f.Date >= startDate && f.Date <= endDate, cancellationToken);
    }

    // Manager MVP addition. Filters by the business Date field (inclusive),
    // consistent with GetCompletedCountAsync above -- NOT by CreatedAt.
    public async Task<IReadOnlyList<Flight>> GetAllAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = _context.Flights.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            query = query.Where(f => f.UserId == userId.Value);

        if (vehicleId.HasValue)
            query = query.Where(f => f.VehicleId == vehicleId.Value);

        if (from.HasValue)
            query = query.Where(f => f.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(f => f.Date <= to.Value);

        return await query.ToListAsync(cancellationToken);
    }

    // Manager MVP pagination addition. Same filters as GetAllAsync above,
    // but Skip/Take run in the database via the query itself -- nothing is
    // loaded into memory before paging. CountAsync runs against the same
    // filtered query (pre-paging) so it reports the true total across all
    // pages, not just the current page's row count. OrderBy is required
    // for Skip/Take to return stable, non-overlapping pages -- Date isn't
    // guaranteed unique, so Id is added as a deterministic tiebreaker.
    public async Task<(IReadOnlyList<Flight> Items, int TotalCount)> GetPagedAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Flights.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            query = query.Where(f => f.UserId == userId.Value);

        if (vehicleId.HasValue)
            query = query.Where(f => f.VehicleId == vehicleId.Value);

        if (from.HasValue)
            query = query.Where(f => f.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(f => f.Date <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(f => f.Date)
            .ThenBy(f => f.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
