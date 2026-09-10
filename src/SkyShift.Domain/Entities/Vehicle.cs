using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Domain.Entities;

/// <summary>
/// An air-transportation vehicle available for the trial program.
/// Assign() enforces the single-assignment rule at the object level: it only
/// succeeds when Status is Available, so a Vehicle that is already InUse (or
/// otherwise unavailable) cannot be assigned again through this method.
/// This guarantees the rule for a sequential call against one loaded instance.
/// It does NOT by itself guarantee the rule under concurrent access (two
/// requests racing to assign the same Vehicle at the same time) — that is a
/// Persistence/Application concern (e.g. optimistic concurrency, a DB
/// constraint, or a transaction) to be addressed when Infrastructure is designed.
/// </summary>
public class Vehicle
{
    public int Id { get; private set; }
    public VehicleStatus Status { get; private set; }
    public bool IsPersonalExperimental { get; private set; }

    /// <summary>
    /// Server-generated creation timestamp (UTC). Always supplied by the
    /// caller (CreateVehicleUseCase, using DateTime.UtcNow) -- never a
    /// client-controlled value. Exists so Manager fleet-growth and future
    /// lifetime-utilization statistics do not depend on any client input.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private readonly List<VehicleAssignment> _vehicleAssignments = new();

    /// <summary>
    /// EF Core navigation side of the Vehicle &lt;-&gt; User many-to-many
    /// relationship -- see User.VehicleAssignments for the full rationale.
    /// Read-only here: populated by EF Core via the private backing field
    /// when eager-loaded, never mutated directly -- assignments are still
    /// created exclusively through SelectVehicleUseCase, unchanged.
    /// </summary>
    public IReadOnlyCollection<VehicleAssignment> VehicleAssignments => _vehicleAssignments.AsReadOnly();

    private Vehicle(bool isPersonalExperimental, DateTime createdAt)
    {
        Status = VehicleStatus.Available;
        IsPersonalExperimental = isPersonalExperimental;
        CreatedAt = createdAt;
    }

    public static Vehicle Create(bool isPersonalExperimental, DateTime createdAt)
    {
        return new Vehicle(isPersonalExperimental, createdAt);
    }

    /// <summary>
    /// Assigns the vehicle to a user for the trial period.
    /// Only legal from Available; otherwise the vehicle is already taken
    /// (or otherwise unusable), which is a business-rule violation.
    /// Creating the related VehicleAssignment record is an Application-layer
    /// orchestration responsibility — see VehicleAssignment.
    /// </summary>
    public void Assign()
    {
        if (Status != VehicleStatus.Available)
            throw new VehicleNotAvailableException(Id, Status);

        Status = VehicleStatus.InUse;
    }

    /// <summary>
    /// Returns the vehicle to the available pool, e.g. once its trial
    /// VehicleAssignment period has ended.
    /// </summary>
    public void Release()
    {
        Status = VehicleStatus.Available;
    }

    /// <summary>
    /// Manager-driven status transitions. Each method enforces its own
    /// legal preconditions -- there is no generic "set status" setter.
    /// None of these can be reached from InUse: a vehicle that is currently
    /// assigned to a user must not have its status forced by a Manager
    /// while that assignment is active (the assignment itself is
    /// unaffected by any of these calls). None of these can be reached
    /// from Retired: Retired is a terminal state in this MVP.
    /// </summary>
    public void Retire()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Retired);

        Status = VehicleStatus.Retired;
    }

    public void MarkAvailable()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Available);

        Status = VehicleStatus.Available;
    }

    public void SendToMaintenance()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Maintenance);

        Status = VehicleStatus.Maintenance;
    }

    public void MarkUnavailable()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Unavailable);

        Status = VehicleStatus.Unavailable;
    }
}
