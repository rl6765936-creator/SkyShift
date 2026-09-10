using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetActiveAssignments;

/// <summary>
/// Manager-wide view of every currently active VehicleAssignment -- who
/// currently has which vehicle, across the whole initiative.
/// </summary>
public class GetActiveAssignmentsUseCase
{
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IMapper _mapper;

    public GetActiveAssignmentsUseCase(IVehicleAssignmentRepository vehicleAssignmentRepository, IMapper mapper)
    {
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AssignmentResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _vehicleAssignmentRepository.GetAllActiveAsync(cancellationToken);
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
