using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Domain.Entities;

/// <summary>
/// A curated road-car model that a Manager may add to the fleet; unlike Vehicle, it has no status or
/// assignments of its own. Every item has seats, a maximum road speed and Hebrew and English
/// descriptions, each specification marked Verified or Illustrative, and no item contains flight
/// specifications.
/// </summary>
public class VehicleCatalogItem
{
    public int Id { get; private set; }
    public string Manufacturer { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = string.Empty;
    public int Seats { get; private set; }
    public SpecificationAccuracy SeatsAccuracy { get; private set; }
    public int MaxRoadSpeedKmh { get; private set; }
    public SpecificationAccuracy MaxRoadSpeedAccuracy { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string DescriptionHe { get; private set; } = string.Empty;

    /// <summary>Root-relative frontend asset path, e.g. /catalog/car-01.webp. Never image bytes or a local path.</summary>
    public string ImageUrl { get; private set; } = string.Empty;

    public CatalogIdentification Identification { get; private set; }

    private VehicleCatalogItem()
    {
    }

    public static VehicleCatalogItem Create(
        int id,
        string manufacturer,
        string modelName,
        int seats,
        SpecificationAccuracy seatsAccuracy,
        int maxRoadSpeedKmh,
        SpecificationAccuracy maxRoadSpeedAccuracy,
        string description,
        string descriptionHe,
        string imageUrl,
        CatalogIdentification identification)
    {
        if (id <= 0)
            throw new InvalidVehicleCatalogException("Catalog item id must be positive.");
        if (string.IsNullOrWhiteSpace(manufacturer) || string.IsNullOrWhiteSpace(modelName))
            throw new InvalidVehicleCatalogException("Manufacturer and model name are required.");
        if (string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(descriptionHe))
            throw new InvalidVehicleCatalogException("English and Hebrew descriptions are required.");
        if (description.Trim().Length > 500 || descriptionHe.Trim().Length > 500)
            throw new InvalidVehicleCatalogException("Descriptions must not exceed 500 characters.");
        if (!Enum.IsDefined(identification))
            throw new InvalidVehicleCatalogException("Unknown catalog identification.");
        if (!Enum.IsDefined(seatsAccuracy) || !Enum.IsDefined(maxRoadSpeedAccuracy))
            throw new InvalidVehicleCatalogException("Unknown specification accuracy.");
        if (identification == CatalogIdentification.VisualInspiration
            && (seatsAccuracy == SpecificationAccuracy.Verified || maxRoadSpeedAccuracy == SpecificationAccuracy.Verified))
            throw new InvalidVehicleCatalogException("Visual-inspiration items can only carry illustrative specifications.");
        if (seats is < 1 or > 100)
            throw new InvalidVehicleCatalogException("Seats must be between 1 and 100.");
        if (maxRoadSpeedKmh is < 1 or > 600)
            throw new InvalidVehicleCatalogException("Maximum road speed must be between 1 and 600 km/h.");
        if (!IsRelativeAssetPath(imageUrl))
            throw new InvalidVehicleCatalogException("Catalog image must be a root-relative frontend asset path.");

        return new VehicleCatalogItem
        {
            Id = id,
            Manufacturer = manufacturer.Trim(),
            ModelName = modelName.Trim(),
            Seats = seats,
            SeatsAccuracy = seatsAccuracy,
            MaxRoadSpeedKmh = maxRoadSpeedKmh,
            MaxRoadSpeedAccuracy = maxRoadSpeedAccuracy,
            Description = description.Trim(),
            DescriptionHe = descriptionHe.Trim(),
            ImageUrl = imageUrl,
            Identification = identification
        };
    }

    /// <summary>
    /// Accepts only paths such as /catalog/car-01.webp: rooted, no scheme,
    /// no protocol-relative host, no Windows separators or drive letters.
    /// </summary>
    public static bool IsRelativeAssetPath(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 260)
            return false;
        if (!value.StartsWith('/') || value.StartsWith("//"))
            return false;
        return !value.Any(c => char.IsWhiteSpace(c) || c == '\\' || c == ':');
    }
}
