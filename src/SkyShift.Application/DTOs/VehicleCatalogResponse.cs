using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

/// <summary>
/// One catalog model still available to add to the fleet. Seats and the
/// maximum road speed are always present; each has its own accuracy flag:
/// Verified (official manufacturer source) or Illustrative (SkyShift sample
/// data, not a manufacturer specification). Propellers in the image are a
/// SkyShift illustration; no flight specification is given or implied.
/// </summary>
public class VehicleCatalogResponse
{
    public int Id { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;

    public int Seats { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SpecificationAccuracy SeatsAccuracy { get; set; }

    // Maximum ROAD speed in km/h.
    public int MaxRoadSpeedKmh { get; set; }

    // Same value as MaxRoadSpeedKmh, kept for the existing React contract.
    public int MaxSpeedKmh { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SpecificationAccuracy MaxRoadSpeedAccuracy { get; set; }

    public string Description { get; set; } = string.Empty;
    public string DescriptionHe { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    // Identification confidence of the manufacturer/model, independent of specification accuracy.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CatalogIdentification Identification { get; set; }
}
