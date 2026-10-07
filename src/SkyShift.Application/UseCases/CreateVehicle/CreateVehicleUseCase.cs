using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.CreateVehicle;

/// <summary>
/// Manager creates a new Vehicle. Status always starts Available (enforced
/// by Vehicle.Create itself, not by this Use Case) and CreatedAt is always
/// DateTime.UtcNow, generated here -- CreateVehicleRequest has no Status or
/// CreatedAt field for a client to supply.
/// </summary>
public class CreateVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateVehicleUseCase(IVehicleRepository vehicleRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _vehicleRepository = vehicleRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ManagerVehicleResponse> ExecuteAsync(CreateVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var vehicle = Vehicle.Create(request.IsPersonalExperimental, DateTime.UtcNow);

        vehicle.SetCatalogDetails(request.ModelName, request.MaxSpeedKmh, request.Seats, request.ImageUrl);

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ManagerVehicleResponse>(vehicle);
    }
}
