using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetAvailableVehicles;

/// <summary>
/// Simple read Use Case — no Domain behavior is invoked, just a Repository
/// query filtered to Status == Available (a Domain-defined value, not an
/// Application-invented filter).
/// </summary>
public class GetAvailableVehiclesUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMapper _mapper;

    public GetAvailableVehiclesUseCase(IVehicleRepository vehicleRepository, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<VehicleResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await _vehicleRepository.GetAvailableAsync(cancellationToken);

        return vehicles
            .Select(v => _mapper.Map<VehicleResponse>(v))
            .ToList();
    }
}
