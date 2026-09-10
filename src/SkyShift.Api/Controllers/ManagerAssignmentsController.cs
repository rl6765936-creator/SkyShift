using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetActiveAssignments;
using SkyShift.Application.UseCases.GetAssignmentHistory;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Manager-wide VehicleAssignment monitoring: every currently active
/// assignment, and ended assignments (optionally filtered). No
/// assignment-mutation endpoints here, by design -- StartDate/EndDate
/// remain exactly as the existing SelectVehicle flow produces them.
/// </summary>
[ApiController]
[Route("api/manager/assignments")]
[Authorize(Roles = "Manager")]
public class ManagerAssignmentsController : ControllerBase
{
    private readonly GetActiveAssignmentsUseCase _getActiveAssignmentsUseCase;
    private readonly GetAssignmentHistoryUseCase _getAssignmentHistoryUseCase;

    public ManagerAssignmentsController(
        GetActiveAssignmentsUseCase getActiveAssignmentsUseCase,
        GetAssignmentHistoryUseCase getAssignmentHistoryUseCase)
    {
        _getActiveAssignmentsUseCase = getActiveAssignmentsUseCase;
        _getAssignmentHistoryUseCase = getAssignmentHistoryUseCase;
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> GetActive(CancellationToken cancellationToken)
    {
        var assignments = await _getActiveAssignmentsUseCase.ExecuteAsync(cancellationToken);
        return Ok(assignments);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> GetHistory(
        [FromQuery] int? userId,
        [FromQuery] int? vehicleId,
        CancellationToken cancellationToken)
    {
        var assignments = await _getAssignmentHistoryUseCase.ExecuteAsync(userId, vehicleId, cancellationToken);
        return Ok(assignments);
    }
}
