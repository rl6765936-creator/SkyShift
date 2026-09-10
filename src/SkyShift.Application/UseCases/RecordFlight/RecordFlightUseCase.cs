using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.RecordFlight;

/// <summary>
/// Records a completed air trip. VehicleId is derived from the user's active
/// VehicleAssignment — never taken from client input. No future-date rule is
/// applied (not an approved business rule); Date only needs API-level format
/// validation, which happens outside this Use Case.
/// </summary>
public class RecordFlightUseCase
{
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordFlightUseCase(
        IVehicleAssignmentRepository vehicleAssignmentRepository,
        IFlightRepository flightRepository,
        IUnitOfWork unitOfWork)
    {
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _flightRepository = flightRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(int userId, RecordFlightRequest request, CancellationToken cancellationToken = default)
    {
        var activeAssignment = await _vehicleAssignmentRepository.GetActiveForUserAsync(userId, cancellationToken);
        if (activeAssignment is null)
            throw new InvalidOperationException("User has no active vehicle assignment.");

        // CreatedAt is a server-generated audit timestamp (UTC), never client
        // input -- distinct from Date, which remains the user-reported business
        // date. See Flight.CreatedAt.
        var flight = Flight.Create(userId, activeAssignment.VehicleId, request.Date, DateTime.UtcNow);
        await _flightRepository.AddAsync(flight, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
