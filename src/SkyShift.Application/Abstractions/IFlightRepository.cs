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

    // Batched form of GetCompletedCountAsync for many assignments at once: for each
    // assignment id, the number of that assignment's user's flights whose Date is
    // within the assignment's own StartDate..EndDate (inclusive). One database query,
    // so callers never loop over assignments issuing a count per row (N+1).
    Task<IReadOnlyDictionary<int, int>> GetCompletedCountsForAssignmentsAsync(IReadOnlyCollection<int> assignmentIds, CancellationToken cancellationToken = default);

    // Total number of flights, counted by the database rather than by loading rows.
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all flights matching the optional filters: userId/vehicleId by exact match, and
    /// from/to by the business Date field (inclusive), not CreatedAt. It is used where the full
    /// filtered set is aggregated (flight statistics and user activity); the paginated list uses
    /// GetPagedAsync.
    /// </summary>
    Task<IReadOnlyList<Flight>> GetAllAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same filters as GetAllAsync, but applies Skip/Take in the database and also returns the
    /// total matching count before paging. page is 1-based, and page and pageSize are validated by
    /// the caller (GetAllFlightsUseCase).
    /// </summary>
    Task<(IReadOnlyList<Flight> Items, int TotalCount)> GetPagedAsync(int? userId, int? vehicleId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
}
