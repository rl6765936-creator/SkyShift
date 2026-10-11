using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.RecordFlight;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Records a flight for the authenticated user. Thin presentation code: it reads the user id from
/// the JWT and calls RecordFlightUseCase, which derives the vehicle from the user's active
/// assignment. Exceptions such as NoActiveAssignmentException propagate to
/// SkyShiftExceptionHandler.
/// </summary>
[ApiController]
[Route("api/flights")]
[Authorize]
public class FlightsController : ControllerBase
{
    private readonly RecordFlightUseCase _recordFlightUseCase;

    public FlightsController(RecordFlightUseCase recordFlightUseCase)
    {
        _recordFlightUseCase = recordFlightUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> RecordFlight([FromBody] RecordFlightRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _recordFlightUseCase.ExecuteAsync(userId, request, cancellationToken);
        return NoContent();
    }
}
