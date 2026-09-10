using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.SelectVehicle;

/// <summary>
/// Orchestrates vehicle selection. Every business decision is delegated to
/// Domain: whether the vehicle can be assigned is decided exclusively by
/// Vehicle.Assign() (this class never inspects vehicle.Status itself), and
/// the trial period's length is decided exclusively by VehicleAssignment.Create().
/// Transaction/concurrency handling for the two writes below is explicitly
/// out of scope here — an Infrastructure/Persistence concern.
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
            throw new InvalidOperationException("User already has an active vehicle assignment.");

        var vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle {request.VehicleId} was not found.");

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
