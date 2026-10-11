namespace SkyShift.Domain.Enums;

public enum VehicleStatus
{
    Available,
    InUse,
    Maintenance,
    Unavailable,

    /// <summary>
    /// Permanently removed from the active fleet, unlike the temporary Unavailable status. A Retired
    /// vehicle can never transition back to any other status.
    /// </summary>
    Retired
}
