namespace SkyShift.Domain.Entities;

/// <summary>
/// Represents the one-month trial relationship between a User and a Vehicle.
/// Kept as its own Aggregate Root (rather than fields on User/Vehicle) so the
/// system retains a history of assignments, not just the currently active one.
/// This Aggregate is intentionally not merged with Vehicle: keeping an active
/// VehicleAssignment consistent with its Vehicle's Status (InUse while active,
/// Available again once released) is a cross-aggregate invariant, so it must
/// be coordinated by an Application-layer Use Case that calls both
/// Vehicle.Assign()/Release() and VehicleAssignment.Create() together.
/// </summary>
public class VehicleAssignment
{
    private const int TrialPeriodMonths = 1;

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int VehicleId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// EF Core reference-navigation side of the many-to-many relationship
    /// this entity realizes between User and Vehicle -- see
    /// User.VehicleAssignments/Vehicle.VehicleAssignments and
    /// VehicleAssignmentConfiguration. Populated by EF Core only when a
    /// query explicitly Include()s it; null! is the standard EF Core
    /// pattern for a required navigation that is always set by the ORM,
    /// never by application code (assignments are still created exclusively
    /// through User.Create/Vehicle.Create + VehicleAssignment.Create in
    /// SelectVehicleUseCase, which never touches these).
    /// </summary>
    public User User { get; private set; } = null!;
    public Vehicle Vehicle { get; private set; } = null!;

    private VehicleAssignment(int userId, int vehicleId, DateTime startDate)
    {
        UserId = userId;
        VehicleId = vehicleId;
        StartDate = startDate;
        EndDate = startDate.AddMonths(TrialPeriodMonths);
    }

    /// <summary>
    /// Business rule: every assignment grants exactly one month of trial use,
    /// starting from the given date.
    /// </summary>
    public static VehicleAssignment Create(int userId, int vehicleId, DateTime startDate)
    {
        return new VehicleAssignment(userId, vehicleId, startDate);
    }

    public bool IsActive(DateTime asOf)
    {
        return asOf >= StartDate && asOf <= EndDate;
    }
}
