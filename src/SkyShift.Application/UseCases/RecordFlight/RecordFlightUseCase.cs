using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.RecordFlight;

/// <summary>Records a flight within the active assignment's trial period.</summary>
public class RecordFlightUseCase
{
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public RecordFlightUseCase(
        IVehicleAssignmentRepository vehicleAssignmentRepository,
        IFlightRepository flightRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _flightRepository = flightRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(int userId, RecordFlightRequest request, CancellationToken cancellationToken = default)
    {
        // Read before GetActiveForUserAsync opens the existing Serializable
        // transaction. This avoids retaining duplicate-check key-range locks
        // while concurrent inserts compete on the unique index.
        var duplicate = await _flightRepository.ExistsAsync(userId, request.Date, cancellationToken);

        var activeAssignment = await _vehicleAssignmentRepository.GetActiveForUserAsync(userId, cancellationToken);
        if (activeAssignment is null)
            throw new InvalidOperationException("User has no active vehicle assignment.");

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        activeAssignment.ValidateFlightDate(request.Date, nowUtc);

        if (duplicate)
            throw new DuplicateFlightException();

        // The database unique index remains the final guard under concurrency.
        var flight = Flight.Create(userId, activeAssignment.VehicleId, request.Date, nowUtc);
        await _flightRepository.AddAsync(flight, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
