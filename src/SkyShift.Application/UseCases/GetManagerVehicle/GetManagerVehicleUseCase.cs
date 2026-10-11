using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.GetManagerVehicle;

/// <summary>
/// Manager view of a single fleet Vehicle (the resource that
/// POST /api/manager/vehicles points to in its Location header). Display read:
/// untracked, no transaction.
/// </summary>
public class GetManagerVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetManagerVehicleUseCase(IVehicleRepository vehicleRepository, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<ManagerVehicleResponse> ExecuteAsync(int vehicleId, CancellationToken cancellationToken = default)
    {
        var vehicle = await _vehicleRepository.GetByIdForReadAsync(vehicleId, cancellationToken)
            ?? throw new VehicleNotFoundException(vehicleId);
        return _mapper.Map<ManagerVehicleResponse>(vehicle);
    }
}
