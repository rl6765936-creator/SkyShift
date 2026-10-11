using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetAvailableVehicles;
using SkyShift.Application.UseCases.GetUserVehicle;
using SkyShift.Application.UseCases.SelectVehicle;

namespace SkyShift.Api.Controllers;

/// <summary>
/// User vehicle endpoints: list available vehicles, create an assignment (POST {id}/assignments,
/// with POST select kept as an alias), and read the caller's active vehicle. Thin presentation
/// code: it reads the user id from the JWT and calls the matching use case. Exceptions propagate to
/// SkyShiftExceptionHandler.
/// </summary>
[ApiController]
[Route("api/vehicles")]
[Authorize]
public class VehiclesController : ControllerBase
{
    private readonly GetAvailableVehiclesUseCase _getAvailableVehiclesUseCase;
    private readonly SelectVehicleUseCase _selectVehicleUseCase;
    private readonly GetUserVehicleUseCase _getUserVehicleUseCase;

    public VehiclesController(
        GetAvailableVehiclesUseCase getAvailableVehiclesUseCase,
        SelectVehicleUseCase selectVehicleUseCase,
        GetUserVehicleUseCase getUserVehicleUseCase)
    {
        _getAvailableVehiclesUseCase = getAvailableVehiclesUseCase;
        _selectVehicleUseCase = selectVehicleUseCase;
        _getUserVehicleUseCase = getUserVehicleUseCase;
    }

    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<VehicleResponse>>> GetAvailable(CancellationToken cancellationToken)
    {
        var vehicles = await _getAvailableVehiclesUseCase.ExecuteAsync(cancellationToken);
        return Ok(vehicles);
    }

    // Resource-oriented route: creates the caller's assignment of vehicle {id}
    // (same use case, rules and responses as the "select" route below).
    [HttpPost("{id:int:min(1)}/assignments")]
    public async Task<IActionResult> CreateAssignment([FromRoute] int id, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _selectVehicleUseCase.ExecuteAsync(userId, new SelectVehicleRequest { VehicleId = id }, cancellationToken);
        return NoContent();
    }

    // Compatibility alias kept for the current React client (POST /api/vehicles/select
    // with { "vehicleId": n }). Prefer POST /api/vehicles/{id}/assignments.
    [HttpPost("select")]
    public async Task<IActionResult> Select([FromBody] SelectVehicleRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _selectVehicleUseCase.ExecuteAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("mine")]
    public async Task<ActionResult<VehicleResponse?>> GetMine(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var vehicle = await _getUserVehicleUseCase.ExecuteAsync(userId, cancellationToken);
        return Ok(vehicle);
    }
}
