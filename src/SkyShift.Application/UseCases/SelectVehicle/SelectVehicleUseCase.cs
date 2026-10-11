using SkyShift.Domain.Exceptions;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.SelectVehicle;

/// <summary>
/// Orchestrates vehicle selection; Vehicle.Assign() decides whether the vehicle can be assigned,
/// and VehicleAssignment.Create() sets the trial period. One active assignment per user is enforced
/// here with ActiveAssignmentExistsException. Transactions and concurrency handling are
/// Infrastructure concerns behind the repositories and IUnitOfWork.
/// </summary>
public class SelectVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SelectVehicleUseCase(
        IVehicleRepository vehicleRepository,
        IVehicleAssignmentRepository vehicleAssignmentRepository,
        IUnitOfWork unitOfWork)
    {
        _vehicleRepository = vehicleRepository;
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int userId, SelectVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var activeAssignment = await _vehicleAssignmentRepository.GetActiveForUserAsync(userId, cancellationToken);
        if (activeAssignment is not null)
            throw new ActiveAssignmentExistsException();

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
            throw new VehicleNotFoundException(request.VehicleId);

        // May throw VehicleNotAvailableException (Domain) — not caught or
        // duplicated here; it propagates to the caller as-is. SaveChangesAsync
        // below may also throw it, translated from a RowVersion conflict
        // (Infrastructure) if another request assigned this Vehicle first.
        vehicle.Assign();
        await _vehicleRepository.UpdateAsync(vehicle, cancellationToken);

        var assignment = VehicleAssignment.Create(userId, vehicle.Id, DateTime.UtcNow);
        await _vehicleAssignmentRepository.AddAsync(assignment, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
