using SkyShift.Domain.Enums;

namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Thrown when a requested status change is not a legal transition from the vehicle's current
/// status, for example retiring an InUse vehicle or setting InUse directly. Unlike
/// VehicleNotAvailableException, it concerns the status change itself, not availability for
/// assignment.
/// </summary>
public sealed class InvalidVehicleStatusTransitionException : DomainException
{
    public InvalidVehicleStatusTransitionException(int vehicleId, VehicleStatus currentStatus, VehicleStatus attemptedStatus)
        : base($"Vehicle {vehicleId} cannot transition from '{currentStatus}' to '{attemptedStatus}'.")
    {
    }
}
