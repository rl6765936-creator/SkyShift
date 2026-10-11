using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetUserVehicle;

/// <summary>
/// Read-only query. No active assignment is a valid, normal outcome (a new
/// user with no vehicle yet) — represented as null, not an exception.
/// </summary>
public class GetUserVehicleUseCase
{
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetUserVehicleUseCase(
        IVehicleAssignmentRepository vehicleAssignmentRepository,
        IVehicleRepository vehicleRepository,
        IMapper mapper)
    {
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<VehicleResponse?> ExecuteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var activeAssignment = await _vehicleAssignmentRepository.GetActiveForUserForReadAsync(userId, cancellationToken);
        if (activeAssignment is null)
            return null;

        var vehicle = await _vehicleRepository.GetByIdForReadAsync(activeAssignment.VehicleId, cancellationToken);
        if (vehicle is null)
            return null;

        return _mapper.Map<VehicleResponse>(vehicle);
    }
}
