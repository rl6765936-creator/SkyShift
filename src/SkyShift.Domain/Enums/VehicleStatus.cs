namespace SkyShift.Domain.Enums;

public enum VehicleStatus
{
    Available,
    InUse,
    Maintenance,
    Unavailable,

    /// <summary>
    /// Permanently removed from the active fleet. Distinct from
    /// Unavailable, which is temporary. A Retired vehicle can never
    /// transition back to any other status in this MVP.
    /// </summary>
    Retired
}
