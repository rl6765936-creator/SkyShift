using SkyShift.Domain.Enums;

namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Thrown when a vehicle cannot be assigned because its status is not Available. UnitOfWork also
/// raises it for the losing side of a concurrent assignment conflict. Mapped to 409 with code
/// vehicle_not_available.
/// </summary>
public sealed class VehicleNotAvailableException : DomainException
{
    public VehicleNotAvailableException(int vehicleId, VehicleStatus currentStatus)
        : base($"Vehicle {vehicleId} is not available for assignment. Current status: {currentStatus}.")
    {
    }
}
