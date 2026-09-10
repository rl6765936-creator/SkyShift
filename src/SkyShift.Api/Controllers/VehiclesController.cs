using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetAvailableVehicles;
using SkyShift.Application.UseCases.GetUserVehicle;
using SkyShift.Application.UseCases.SelectVehicle;

namespace SkyShift.Api.Controllers;

/// <summary>
/// MVP vehicle endpoints: list available vehicles, select one, and read
/// the caller's own active vehicle. Thin presentation code only -- it
/// extracts the authenticated UserId from the JWT claim, calls the
/// appropriate Application Use Case, and returns the HTTP response. No
/// business logic, no repository/DbContext access, and no exception
/// handling of its own: KeyNotFoundException, InvalidOperationException,
/// and VehicleNotAvailableException are all left to propagate to the
/// existing global SkyShiftExceptionHandler.
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
