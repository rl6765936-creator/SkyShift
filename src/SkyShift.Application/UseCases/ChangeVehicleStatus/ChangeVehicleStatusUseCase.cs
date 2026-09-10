using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.ChangeVehicleStatus;

/// <summary>
/// Manager-driven Vehicle status change, including Retire. Every business
/// decision about which transitions are legal is delegated to Domain (the
/// Vehicle.Retire()/MarkAvailable()/SendToMaintenance()/MarkUnavailable()
/// methods) -- this Use Case never inspects vehicle.Status itself, and does
/// not catch InvalidVehicleStatusTransitionException; it propagates to the
/// caller as-is, to be mapped by the global exception handler.
///
/// InUse is not a Manager-settable target status: it is only ever reached
/// through Vehicle.Assign(), as part of the existing SelectVehicle flow.
/// Requesting InUse here is rejected the same way an illegal transition is.
/// </summary>
public class ChangeVehicleStatusUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ChangeVehicleStatusUseCase(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ManagerVehicleResponse> ExecuteAsync(int vehicleId, ChangeVehicleStatusRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdForManagementAsync(vehicleId, cancellationToken);
        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle {vehicleId} was not found.");

        switch (request.TargetStatus)
        {
            case VehicleStatus.Available:
                vehicle.MarkAvailable();
                break;
            case VehicleStatus.Maintenance:
                vehicle.SendToMaintenance();
                break;
            case VehicleStatus.Unavailable:
                vehicle.MarkUnavailable();
                break;
            case VehicleStatus.Retired:
                vehicle.Retire();
                break;
            case VehicleStatus.InUse:
                throw new InvalidVehicleStatusTransitionException(vehicle.Id, vehicle.Status, VehicleStatus.InUse);
            default:
                throw new ArgumentOutOfRangeException(nameof(request), "Unrecognized target status.");
        }

        await _vehicleRepository.UpdateStatusAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ManagerVehicleResponse>(vehicle);
    }
}
