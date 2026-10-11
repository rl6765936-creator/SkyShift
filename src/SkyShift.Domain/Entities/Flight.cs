namespace SkyShift.Domain.Entities;

/// <summary>
/// A single recorded air trip performed by a user. Deliberately minimal —
/// it is a fact record, not a rich behavioral entity; the business rules that
/// consume it (expected-vs-completed trips, lottery eligibility) live in
/// User and LotteryEligibilityService, not here.
/// </summary>
public class Flight
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int? VehicleId { get; private set; }

    /// <summary>
    /// The date and time the user reports for the flight; it must be UTC and within the active
    /// assignment's trial window (VehicleAssignment.ValidateFlightDate). Completed-flight counts use
    /// it, and it is never replaced by CreatedAt.
    /// </summary>
    public DateTime Date { get; private set; }

    /// <summary>
    /// Server-generated UTC timestamp of when this Flight record was created, always supplied by
    /// RecordFlightUseCase and never by the client. Used for Manager activity trends because, unlike
    /// Date, it cannot be manipulated.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private Flight(int userId, int? vehicleId, DateTime date, DateTime createdAt)
    {
        UserId = userId;
        VehicleId = vehicleId;
        Date = date;
        CreatedAt = createdAt;
    }

    public static Flight Create(int userId, int? vehicleId, DateTime date, DateTime createdAt)
    {
        return new Flight(userId, vehicleId, date, createdAt);
    }
}
