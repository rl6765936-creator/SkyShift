using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.CreateVehicle;

/// <summary>
/// Creates an Available fleet vehicle from an unused catalog model, with a server CreatedAt and all
/// metadata copied from the catalog item. IsInUseAsync gives an early 409, and if two requests race
/// past it the unique index IX_Vehicles_CatalogItemId lets only one commit (the other becomes
/// CatalogItemInUseException).
/// </summary>
public class CreateVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleCatalogRepository _catalogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateVehicleUseCase(
        IVehicleRepository vehicleRepository,
        IVehicleCatalogRepository catalogRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _catalogRepository = catalogRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ManagerVehicleResponse> ExecuteAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _catalogRepository.GetByIdAsync(request.CatalogItemId, cancellationToken)
            ?? throw new CatalogItemNotFoundException(request.CatalogItemId);

        if (await _catalogRepository.IsInUseAsync(item.Id, cancellationToken))
            throw new CatalogItemInUseException();

        var vehicle = Vehicle.Create(request.IsPersonalExperimental, DateTime.UtcNow);
        vehicle.ApplyCatalogItem(item);

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ManagerVehicleResponse>(vehicle);
    }
}
