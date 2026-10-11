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

    public Task<bool> ExistsAsync(int userId, DateTime date, CancellationToken cancellationToken = default)
    {
        return _context.Flights.AsNoTracking()
            .AnyAsync(f => f.UserId == userId && f.Date == date, cancellationToken);
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

    // Same date rule as GetCompletedCountAsync (UserId match, Date within the
    // assignment's StartDate..EndDate inclusive), evaluated per assignment by the
    // database in a single query: a correlated COUNT subquery per assignment row.
    public async Task<IReadOnlyDictionary<int, int>> GetCompletedCountsForAssignmentsAsync(IReadOnlyCollection<int> assignmentIds, CancellationToken cancellationToken = default)
    {
        if (assignmentIds.Count == 0)
            return new Dictionary<int, int>();

        return await _context.VehicleAssignments
            .AsNoTracking()
            .Where(a => assignmentIds.Contains(a.Id))
            .Select(a => new
            {
                a.Id,
                Completed = _context.Flights.Count(f => f.UserId == a.UserId && f.Date >= a.StartDate && f.Date <= a.EndDate)
            })
            .ToDictionaryAsync(x => x.Id, x => x.Completed, cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Flights.CountAsync(cancellationToken);
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
