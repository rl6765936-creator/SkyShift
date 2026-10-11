using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.ChangeVehicleStatus;

/// <summary>
/// Manager-driven vehicle status change, including Retire; the Domain's Vehicle methods decide which
/// transitions are legal and throw InvalidVehicleStatusTransitionException otherwise. InUse cannot be
/// requested here because it is only reached through Vehicle.Assign() during vehicle selection.
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
            throw new VehicleNotFoundException(vehicleId);

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
