using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

// Separate from the existing VehicleResponse (used by the 7 User
// endpoints) so that adding CreatedAt here never touches the existing
// User-facing contract.
public class ManagerVehicleResponse
{
    // Catalog metadata (null for legacy, unlinked vehicles).
    public int? CatalogItemId { get; set; }
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public string? DescriptionHe { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatalogIdentification? CatalogIdentification { get; set; }

    public string? ModelName { get; set; }

    // See VehicleResponse.MaxSpeedKmh / MaxRoadSpeedKmh.
    public int? MaxSpeedKmh { get; set; }
    public int? MaxRoadSpeedKmh { get; set; }

    // Verified / Illustrative for the catalog-copied Seats and MaxRoadSpeedKmh.
    // Null for legacy vehicles (their values are free-form and unclassified).
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SpecificationAccuracy? SeatsAccuracy { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SpecificationAccuracy? MaxRoadSpeedAccuracy { get; set; }

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
