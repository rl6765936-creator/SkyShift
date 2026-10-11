using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.CreateVehicle;

/// <summary>
/// Creates a fleet vehicle from an unused catalog model; it starts Available, gets a server
/// CreatedAt, and copies all metadata from the catalog item. IsInUseAsync gives a clear early 409
/// when the model is already used. If two requests race past that check, the unique index
/// IX_Vehicles_CatalogItemId lets only one commit, and UnitOfWork reports the other as
/// CatalogItemInUseException.
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
