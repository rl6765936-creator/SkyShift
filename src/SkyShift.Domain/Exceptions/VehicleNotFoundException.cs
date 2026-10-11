namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Expected not-found result for a Vehicle id. Derives from KeyNotFoundException,
/// like CatalogItemNotFoundException. Maps to 404; a bare KeyNotFoundException
/// (an unexpected bug) stays a 500.
/// </summary>
public sealed class VehicleNotFoundException : KeyNotFoundException
{
    public VehicleNotFoundException(int vehicleId)
        : base($"Vehicle {vehicleId} was not found.")
    {
        VehicleId = vehicleId;
    }

    public int VehicleId { get; }
}
