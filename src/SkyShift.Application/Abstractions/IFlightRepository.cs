using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IFlightRepository
{
    Task<bool> ExistsAsync(int userId, DateTime date, CancellationToken cancellationToken = default);

    Task AddAsync(Flight flight, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts flights recorded by the given user with a Date between
    /// startDate and endDate (inclusive) — used to count completed flights
    /// within a specific VehicleAssignment's trial period.
    /// </summary>
    Task<int> GetCompletedCountAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manager MVP addition. Returns all Flights matching the given
    /// optional filters. userId/vehicleId filter by exact match; from/to
    /// filter by the business Date field (inclusive), consistent with
    /// GetCompletedCountAsync above -- NOT by CreatedAt. Used both for the
    /// raw Manager flight list and, in Application, as the source data for
    /// Manager flight statistics (grouping/aggregation happens in the Use
    /// Case, not here).
    /// </summary>
    Task<IReadOnlyList<Flight>> GetAllAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manager MVP pagination addition. Same optional filters as
    /// GetAllAsync above, but applies Skip/Take in the database and also
    /// returns the total matching row count (pre-paging), so the caller
    /// can report how many pages exist without loading them. page is
    /// 1-based; both page and pageSize are assumed already validated by
    /// the caller (GetAllFlightsUseCase). Used only for the paginated
    /// Manager flight list -- GetAllAsync above is untouched and is still
    /// used, unpaginated, by the aggregate/statistics/dashboard Use Cases
    /// that need the full filtered set.
    /// </summary>
    Task<(IReadOnlyList<Flight> Items, int TotalCount)> GetPagedAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
}
