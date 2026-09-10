using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.ChangeVehicleStatus;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.GetAllVehicles;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Manager fleet management: create vehicles and change their status.
/// Thin presentation code only, exactly like the existing User-facing
/// controllers -- it calls Application Use Cases and maps the result to an
/// HTTP response. Status/CreatedAt are never accepted from the client:
/// CreateVehicleRequest has no such fields, and ChangeVehicleStatusRequest
/// only carries the intended TargetStatus, with the actual legality of the
/// transition decided entirely by Domain (Vehicle's guarded methods).
/// InvalidVehicleStatusTransitionException and KeyNotFoundException are
/// left to propagate to the existing global SkyShiftExceptionHandler.
/// </summary>
[ApiController]
[Route("api/manager/vehicles")]
[Authorize(Roles = "Manager")]
public class ManagerVehiclesController : ControllerBase
{
    private readonly CreateVehicleUseCase _createVehicleUseCase;
    private readonly GetAllVehiclesUseCase _getAllVehiclesUseCase;
    private readonly ChangeVehicleStatusUseCase _changeVehicleStatusUseCase;

    public ManagerVehiclesController(
        CreateVehicleUseCase createVehicleUseCase,
        GetAllVehiclesUseCase getAllVehiclesUseCase,
        ChangeVehicleStatusUseCase changeVehicleStatusUseCase)
    {
        _createVehicleUseCase = createVehicleUseCase;
        _getAllVehiclesUseCase = getAllVehiclesUseCase;
        _changeVehicleStatusUseCase = changeVehicleStatusUseCase;
    }

    // 201 Created carries a Location header pointing at the created Vehicle
    // (no GET-by-id endpoint exists on this controller to route through, so
    // the URI is built directly -- the response body/status code are
    // otherwise unchanged).
    [HttpPost]
    public async Task<ActionResult<ManagerVehicleResponse>> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await _createVehicleUseCase.ExecuteAsync(request, cancellationToken);
        return Created($"/api/manager/vehicles/{vehicle.Id}", vehicle);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ManagerVehicleResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var vehicles = await _getAllVehiclesUseCase.ExecuteAsync(cancellationToken);
        return Ok(vehicles);
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ManagerVehicleResponse>> ChangeStatus([FromRoute] int id, [FromBody] ChangeVehicleStatusRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await _changeVehicleStatusUseCase.ExecuteAsync(id, request, cancellationToken);
        return Ok(vehicle);
    }
}
