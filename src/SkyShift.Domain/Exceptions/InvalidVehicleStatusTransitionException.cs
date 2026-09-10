using SkyShift.Domain.Enums;

namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Thrown when a Manager-driven Vehicle status change is not a legal
/// transition from the vehicle's current status (e.g. attempting to Retire
/// a vehicle that is currently InUse, or attempting to set InUse directly
/// instead of through Assign()). Distinct from VehicleNotAvailableException,
/// which specifically means "not available for assignment" -- this
/// exception means "this status change itself is not a legal transition".
/// </summary>
public sealed class InvalidVehicleStatusTransitionException : DomainException
{
    public InvalidVehicleStatusTransitionException(int vehicleId, VehicleStatus currentStatus, VehicleStatus attemptedStatus)
        : base($"Vehicle {vehicleId} cannot transition from '{currentStatus}' to '{attemptedStatus}'.")
    {
    }
}
