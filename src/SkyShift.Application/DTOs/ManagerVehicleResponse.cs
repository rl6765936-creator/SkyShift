using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

// Separate from the existing VehicleResponse (used by the 7 User
// endpoints) so that adding CreatedAt here never touches the existing
// User-facing contract.
public class ManagerVehicleResponse
{
    public string? ModelName { get; set; }
    public int? MaxSpeedKmh { get; set; }
    public int? Seats { get; set; }
    public string? ImageUrl { get; set; }

    public int Id { get; set; }

    // Serialized as its name ("Available", "Retired", ...), not the
    // underlying int -- scoped to this Manager-only DTO via a
    // property-level JsonConverter, so the existing VehicleResponse.Status
    // (used by the 7 User endpoints) is completely unaffected and keeps
    // serializing as an integer exactly as before.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehicleStatus Status { get; set; }

    public bool IsPersonalExperimental { get; set; }
    public DateTime CreatedAt { get; set; }
}
