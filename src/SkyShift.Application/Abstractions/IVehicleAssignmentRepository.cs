using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IVehicleAssignmentRepository
{
    /// <summary>
    /// Returns the user's currently active assignment (the Domain's VehicleAssignment.ActiveAt
    /// rule), or null. It joins the Serializable transaction used by the SelectVehicle and
    /// RecordFlight write paths; display reads use GetActiveForUserForReadAsync.
    /// </summary>
    Task<VehicleAssignment?> GetActiveForUserAsync(int userId, CancellationToken cancellationToken = default);

    // Display-only variant of GetActiveForUserAsync (same active-window rule) that
    // does not open the SelectVehicle Serializable transaction.
    Task<VehicleAssignment?> GetActiveForUserForReadAsync(int userId, CancellationToken cancellationToken = default);

    Task AddAsync(VehicleAssignment assignment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every currently active assignment across all users (the Domain's ActiveAt rule). It
    /// is a read-only Manager query and does not open the SelectVehicle transaction.
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
