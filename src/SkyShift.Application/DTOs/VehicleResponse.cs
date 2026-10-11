using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

public class VehicleResponse
{
    // Catalog metadata (null for legacy, unlinked vehicles).
    public int? CatalogItemId { get; set; }
    public string? Manufacturer { get; set; }
    public string? Description { get; set; }
    public string? DescriptionHe { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatalogIdentification? CatalogIdentification { get; set; }

    public string? ModelName { get; set; }

    // Kept for compatibility. For catalog-linked vehicles it equals
    // MaxRoadSpeedKmh; for legacy vehicles it is the old free-form value.
    public int? MaxSpeedKmh { get; set; }

    // Maximum ROAD speed, only for catalog-linked vehicles. Never a flight speed.
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
    public VehicleStatus Status { get; set; }
    public bool IsPersonalExperimental { get; set; }
}
