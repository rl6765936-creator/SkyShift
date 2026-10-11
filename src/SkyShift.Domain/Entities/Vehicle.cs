using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Domain.Entities;

/// <summary>
/// A fleet vehicle that users can be assigned for a trial period; Assign() succeeds only while the
/// status is Available. Concurrent assignment is handled in Infrastructure (a row lock in the
/// critical-path transaction plus a concurrency token).
/// </summary>
public class Vehicle
{
    public int Id { get; private set; }

    /// <summary>
    /// The catalog model this vehicle was created from. Null for legacy
    /// vehicles created before the catalog existed; they stay unlinked
    /// until an explicit, reviewed mapping is applied.
    /// </summary>
    public int? CatalogItemId { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Description { get; private set; }
    public string? DescriptionHe { get; private set; }
    public CatalogIdentification? CatalogIdentification { get; private set; }

    /// <summary>
    /// Accuracy of Seats / MaxSpeedKmh copied from the catalog. Null for
    /// legacy vehicles and for vehicles created before accuracy was tracked.
    /// </summary>
    public SpecificationAccuracy? SeatsAccuracy { get; private set; }
    public SpecificationAccuracy? MaxRoadSpeedAccuracy { get; private set; }

    public string? ModelName { get; private set; }

    /// <summary>
    /// For catalog-linked vehicles this is the catalog's maximum ROAD speed
    /// (see MaxRoadSpeedAccuracy for verified vs illustrative). Legacy
    /// vehicles keep whatever value was entered before the catalog existed.
    /// </summary>
    public int? MaxSpeedKmh { get; private set; }
    public int? Seats { get; private set; }
    public string? ImageUrl { get; private set; }

    public VehicleStatus Status { get; private set; }
    public bool IsPersonalExperimental { get; private set; }

    /// <summary>
    /// Server-generated UTC creation timestamp, always supplied by CreateVehicleUseCase and never by
    /// the client. It lets Manager fleet statistics avoid depending on client input.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private readonly List<VehicleAssignment> _vehicleAssignments = new();

    /// <summary>
    /// EF Core navigation side of the Vehicle &lt;-&gt; User many-to-many
    /// relationship -- see User.VehicleAssignments for the full rationale.
    /// Read-only here: populated by EF Core via the private backing field
    /// when eager-loaded, never mutated directly -- assignments are still
    /// created exclusively through SelectVehicleUseCase, unchanged.
    /// </summary>
    public IReadOnlyCollection<VehicleAssignment> VehicleAssignments => _vehicleAssignments.AsReadOnly();

    private Vehicle(bool isPersonalExperimental, DateTime createdAt)
    {
        Status = VehicleStatus.Available;
        IsPersonalExperimental = isPersonalExperimental;
        CreatedAt = createdAt;
    }

    public static Vehicle Create(bool isPersonalExperimental, DateTime createdAt)
    {
        return new Vehicle(isPersonalExperimental, createdAt);
    }

    /// <summary>
    /// Assigns the vehicle to a user for the trial period; only legal from Available, otherwise the
    /// vehicle is taken or unusable. Creating the related VehicleAssignment record is the Application
    /// layer's responsibility.
    /// </summary>
    public void Assign()
    {
        if (Status != VehicleStatus.Available)
            throw new VehicleNotAvailableException(Id, Status);

        Status = VehicleStatus.InUse;
    }

    /// <summary>
    /// Returns the vehicle to the available pool, e.g. once its trial
    /// VehicleAssignment period has ended.
    /// </summary>
    public void Release()
    {
        Status = VehicleStatus.Available;
    }

    /// <summary>
    /// Manager-driven status transitions; each method enforces its own preconditions, and there is no
    /// generic status setter. None of them can start from InUse while an assignment is active, and
    /// Retired is terminal.
    /// </summary>
    public void Retire()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Retired);

        Status = VehicleStatus.Retired;
    }

    public void MarkAvailable()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Available);

        Status = VehicleStatus.Available;
    }

    public void SendToMaintenance()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Maintenance);

        Status = VehicleStatus.Maintenance;
    }

    public void MarkUnavailable()
    {
        if (Status == VehicleStatus.InUse || Status == VehicleStatus.Retired)
            throw new InvalidVehicleStatusTransitionException(Id, Status, VehicleStatus.Unavailable);

        Status = VehicleStatus.Unavailable;
    }
    /// <summary>Optional catalog data; legacy vehicles retain unknown (null) values.</summary>
    public void SetCatalogDetails(string? modelName, int? maxSpeedKmh, int? seats, string? imageUrl)
    {
        if (CatalogItemId.HasValue)
            throw new InvalidVehicleCatalogException("Catalog-linked vehicle details come from the catalog and cannot be edited.");

        var name = modelName?.Trim();
        var image = imageUrl?.Trim();
        if (name is not null && (name.Length == 0 || name.Length > 200))
            throw new InvalidVehicleCatalogException("Model name must contain 1 to 200 characters.");
        if (maxSpeedKmh.HasValue && (maxSpeedKmh < 1 || maxSpeedKmh > 2000))
            throw new InvalidVehicleCatalogException("Maximum speed must be between 1 and 2000 km/h.");
        if (seats.HasValue && (seats < 1 || seats > 100))
            throw new InvalidVehicleCatalogException("Seats must be between 1 and 100.");
        if (image is not null && (image.Length == 0 || image.Length > 2048 || !IsValidImageUrl(image)))
            throw new InvalidVehicleCatalogException("Image URL must be an HTTP(S) URL or a root-relative asset path.");

        ModelName = name;
        MaxSpeedKmh = maxSpeedKmh;
        Seats = seats;
        ImageUrl = image;
    }

    /// <summary>
    /// Links this vehicle to a catalog model and copies the catalog's
    /// authoritative metadata. Allowed once, and only for a vehicle that has
    /// no metadata yet, so existing data is never silently replaced.
    /// </summary>
    public void ApplyCatalogItem(VehicleCatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (CatalogItemId.HasValue)
            throw new InvalidVehicleCatalogException("A vehicle's catalog model cannot be replaced.");
        if (ModelName is not null || MaxSpeedKmh.HasValue || Seats.HasValue || ImageUrl is not null)
            throw new InvalidVehicleCatalogException("This vehicle already has its own details and cannot be linked automatically.");

        CatalogItemId = item.Id;
        Manufacturer = item.Manufacturer;
        ModelName = item.ModelName;
        MaxSpeedKmh = item.MaxRoadSpeedKmh;
        Seats = item.Seats;
        SeatsAccuracy = item.SeatsAccuracy;
        MaxRoadSpeedAccuracy = item.MaxRoadSpeedAccuracy;
        ImageUrl = item.ImageUrl;
        Description = item.Description;
        DescriptionHe = item.DescriptionHe;
        CatalogIdentification = item.Identification;
    }

    private static bool IsValidImageUrl(string value)
    {
        if (value.Any(char.IsWhiteSpace) || value.Contains('\\'))
            return false;
        if (value.StartsWith('/') && !value.StartsWith("//"))
            return true;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
