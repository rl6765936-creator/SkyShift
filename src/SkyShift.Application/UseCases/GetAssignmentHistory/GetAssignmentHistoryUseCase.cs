using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetAssignmentHistory;

/// <summary>
/// Manager-wide view of ended VehicleAssignments (EndDate in the past),
/// optionally filtered by user and/or vehicle.
/// </summary>
public class GetAssignmentHistoryUseCase
{
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IMapper _mapper;

    public GetAssignmentHistoryUseCase(IVehicleAssignmentRepository vehicleAssignmentRepository, IMapper mapper)
    {
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AssignmentResponse>> ExecuteAsync(int? userId, int? vehicleId, CancellationToken cancellationToken = default)
    {
        var assignments = await _vehicleAssignmentRepository.GetHistoryAsync(userId, vehicleId, cancellationToken);
        var now = DateTime.UtcNow;

        return assignments
            .Select(a =>
            {
                var response = _mapper.Map<AssignmentResponse>(a);
                response.IsActive = a.IsActive(now);
                return response;
            })
            .ToList();
    }
}
