using System.Linq.Expressions;

namespace SkyShift.Domain.Entities;

/// <summary>
/// The one-month trial relationship between a User and a Vehicle, kept as its own aggregate so
/// assignment history is preserved. Keeping it consistent with the vehicle's status is a
/// cross-aggregate rule coordinated by SelectVehicleUseCase, which calls Vehicle.Assign() and
/// VehicleAssignment.Create() together.
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
    /// EF Core reference navigation for the User-Vehicle relationship that this entity realizes. It
    /// is populated only when a query includes it; null! is the usual pattern for a required
    /// navigation set by the ORM.
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

    // The trial-window rule: active from StartDate to EndDate, both inclusive.
    public bool IsActive(DateTime asOf)
    {
        return asOf >= StartDate && asOf <= EndDate;
    }

    // Query forms of the same rule (plain expression trees, no ORM dependency), so
    // repositories filter with the Domain's definition instead of restating it.
    // Kept next to IsActive; a unit test asserts they agree on every boundary.
    public static Expression<Func<VehicleAssignment, bool>> ActiveAt(DateTime asOf) =>
        a => asOf >= a.StartDate && asOf <= a.EndDate;

    public static Expression<Func<VehicleAssignment, bool>> EndedBefore(DateTime asOf) =>
        a => a.EndDate < asOf;
    /// <summary>Uses the same inclusive trial boundaries as IsActive.</summary>
    public void ValidateFlightDate(DateTime flightDate, DateTime nowUtc)
    {
        if (flightDate.Kind != DateTimeKind.Utc)
            throw new SkyShift.Domain.Exceptions.InvalidFlightDateException(
                "flight_time_zone_required", "Flight date must include a UTC time zone (Z).");

        if (flightDate < StartDate || flightDate > EndDate)
            throw new SkyShift.Domain.Exceptions.InvalidFlightDateException(
                "flight_outside_trial", "Flight date must be within the vehicle assignment trial period.");

        if (flightDate > nowUtc)
            throw new SkyShift.Domain.Exceptions.InvalidFlightDateException(
                "flight_in_future", "Flight date cannot be in the future.");
    }
}
