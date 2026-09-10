using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IVehicleAssignmentRepository
{
    /// <summary>
    /// Returns the VehicleAssignment that is currently active for the given
    /// user (i.e. satisfies VehicleAssignment.IsActive as of now), or null if
    /// the user has none. The "active" filtering mirrors Domain's IsActive
    /// semantics and is implemented as a Persistence/Infrastructure concern —
    /// Application relies on this contract rather than re-deriving it.
    /// </summary>
    Task<VehicleAssignment?> GetActiveForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task AddAsync(VehicleAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manager MVP addition. Returns every VehicleAssignment that is
    /// currently active (mirrors IsActive semantics), across all users --
    /// not scoped to one user like GetActiveForUserAsync. Deliberately NOT
    /// part of the SelectVehicle critical path (no ambient transaction):
    /// Manager monitoring is a read-only concern, unrelated to the
    /// SelectVehicle concurrency race.
    /// </summary>
    Task<IReadOnlyList<VehicleAssignment>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Manager MVP addition. Returns every VehicleAssignment that has
    /// already ended (EndDate in the past), optionally filtered by user
    /// and/or vehicle. Together with GetAllActiveAsync, covers every
    /// VehicleAssignment row exactly once (an assignment is always either
    /// currently active or already ended).
    /// </summary>
    Task<IReadOnlyList<VehicleAssignment>> GetHistoryAsync(int? userId, int? vehicleId, CancellationToken cancellationToken = default);
}
