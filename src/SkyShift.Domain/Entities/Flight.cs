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
    /// Business date reported by the user -- the date they claim the flight
    /// occurred. Client-supplied, format-validated only. Used wherever
    /// business logic explicitly means "the date of the flight" (e.g.
    /// LotteryEligibilityService completion counting). Never replaced by
    /// CreatedAt.
    /// </summary>
    public DateTime Date { get; private set; }

    /// <summary>
    /// Server-generated timestamp (UTC) marking when this Flight record was
    /// actually created. Always supplied by the caller (RecordFlightUseCase,
    /// using DateTime.UtcNow) -- never a client-controlled value. Used for
    /// Manager activity trends and audit-oriented statistics, precisely
    /// because it cannot be manipulated the way Date can.
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
