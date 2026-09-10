namespace SkyShift.Application.DTOs;

public class FlightResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? VehicleId { get; set; }

    // Business date reported by the user. See Flight.Date.
    public DateTime Date { get; set; }

    // Server-generated audit timestamp. See Flight.CreatedAt.
    public DateTime CreatedAt { get; set; }
}
