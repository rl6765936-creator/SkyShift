using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.RecordFlight;

namespace SkyShift.Api.Controllers;

/// <summary>
/// MVP flight-recording endpoint. Thin presentation code only -- it
/// extracts the authenticated UserId from the JWT claim and calls
/// RecordFlightUseCase, which alone derives the VehicleId from the user's
/// active assignment. No business logic, no repository/DbContext access,
/// and no exception handling of its own: InvalidOperationException (no
/// active assignment) is left to propagate to the existing global
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
