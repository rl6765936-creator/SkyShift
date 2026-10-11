using SkyShift.Application.Abstractions;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.DeleteVehicle;

/// <summary>
/// Deletes a fleet vehicle only while it is Available and has no assignment or flight history;
/// vehicles with history are kept (retire them instead) and nothing is cascaded. The vehicle is
/// read with an update lock in the same Serializable transaction as the history check, and the
/// DELETE carries its RowVersion, so a concurrent assignment and deletion cannot both succeed.
/// Deleting the row returns its catalog model to the creation list, because a model counts as used
/// only while a vehicle row references it.
/// </summary>
public class DeleteVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteVehicleUseCase(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId, cancellationToken)
            ?? throw new VehicleNotFoundException(vehicleId);

        if (vehicle.Status != VehicleStatus.Available)
            throw VehicleDeletionNotAllowedException.NotAvailable(vehicle.Id, vehicle.Status);

        if (await _vehicleRepository.HasHistoryAsync(vehicle.Id, cancellationToken))
            throw VehicleDeletionNotAllowedException.HasHistory(vehicle.Id);

        await _vehicleRepository.DeleteAsync(vehicle, cancellationToken);
        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (VehicleNotAvailableException)
        {
            // UnitOfWork reports a RowVersion conflict or a serialization failure on a
            // Vehicle as VehicleNotAvailableException; for a delete it means the vehicle
            // changed (e.g. was just assigned) after it was read.
            throw VehicleDeletionNotAllowedException.ChangedConcurrently(vehicle.Id);
        }
    }
}
