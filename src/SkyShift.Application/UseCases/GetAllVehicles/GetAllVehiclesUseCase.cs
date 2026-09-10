using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetAllVehicles;

/// <summary>
/// Manager fleet listing -- every Vehicle regardless of Status, including
/// Retired. Distinct from the existing GetAvailableVehiclesUseCase, which
/// intentionally filters to Status == Available for the User-facing
/// endpoint.
/// </summary>
public class GetAllVehiclesUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetAllVehiclesUseCase(IVehicleRepository vehicleRepository, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ManagerVehicleResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetAllAsync(cancellationToken);

        return vehicles
            .Select(v => _mapper.Map<ManagerVehicleResponse>(v))
            .ToList();
    }
}
