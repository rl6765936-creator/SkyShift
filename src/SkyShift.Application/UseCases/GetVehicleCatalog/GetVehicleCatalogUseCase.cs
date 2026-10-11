using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetVehicleCatalog;

/// <summary>
/// Manager read: catalog models that can still be added to the fleet. A model leaves this list once
/// a vehicle references it, even a retired one, and returns only if that never-used vehicle is
/// deleted.
/// </summary>
public class GetVehicleCatalogUseCase
{
    private readonly IVehicleCatalogRepository _catalogRepository;
    private readonly IMapper _mapper;

    public GetVehicleCatalogUseCase(IVehicleCatalogRepository catalogRepository, IMapper mapper)
    {
        _catalogRepository = catalogRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<VehicleCatalogResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var items = await _catalogRepository.GetUnusedAsync(cancellationToken);

        return items
            .Select(item => _mapper.Map<VehicleCatalogResponse>(item))
            .ToList();
    }
}
