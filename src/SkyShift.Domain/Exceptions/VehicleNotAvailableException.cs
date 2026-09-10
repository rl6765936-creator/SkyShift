using SkyShift.Domain.Enums;

namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Thrown when an attempt is made to assign a Vehicle that is not currently
/// in the Available status. Covers both a vehicle that is generally unavailable
/// and a personal-experimental vehicle that is already assigned to another user —
/// both cases reduce to the same underlying violation: the vehicle's Status is
/// not Available at the moment of assignment.
/// </summary>
public sealed class VehicleNotAvailableException : DomainException
{
    public VehicleNotAvailableException(int vehicleId, VehicleStatus currentStatus)
        : base($"Vehicle {vehicleId} is not available for assignment. Current status: {currentStatus}.")
    {
    }
}
