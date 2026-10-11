using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.ChangeVehicleStatus;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.DeleteVehicle;
using SkyShift.Application.UseCases.GetAllVehicles;
using SkyShift.Application.UseCases.GetManagerVehicle;
using SkyShift.Application.UseCases.GetVehicleCatalog;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Manager fleet management: list unused catalog models, create a vehicle from one, read or delete
/// a vehicle, and change its status. Thin presentation code that calls use cases; Status and
/// CreatedAt are never accepted from the client, and transition rules live in the Domain's Vehicle
/// methods. Exceptions propagate to SkyShiftExceptionHandler.
/// </summary>
[ApiController]
[Route("api/manager/vehicles")]
[Authorize(Roles = "Manager")]
public class ManagerVehiclesController : ControllerBase
{
    private readonly CreateVehicleUseCase _createVehicleUseCase;
    private readonly GetAllVehiclesUseCase _getAllVehiclesUseCase;
    private readonly ChangeVehicleStatusUseCase _changeVehicleStatusUseCase;
    private readonly GetVehicleCatalogUseCase _getVehicleCatalogUseCase;
    private readonly GetManagerVehicleUseCase _getManagerVehicleUseCase;
    private readonly DeleteVehicleUseCase _deleteVehicleUseCase;

    public const string GetByIdRouteName = "GetManagerVehicleById";

    public ManagerVehiclesController(
        CreateVehicleUseCase createVehicleUseCase,
        GetAllVehiclesUseCase getAllVehiclesUseCase,
        ChangeVehicleStatusUseCase changeVehicleStatusUseCase,
        GetVehicleCatalogUseCase getVehicleCatalogUseCase,
        GetManagerVehicleUseCase getManagerVehicleUseCase,
        DeleteVehicleUseCase deleteVehicleUseCase)
    {
        _createVehicleUseCase = createVehicleUseCase;
        _getAllVehiclesUseCase = getAllVehiclesUseCase;
        _changeVehicleStatusUseCase = changeVehicleStatusUseCase;
        _getVehicleCatalogUseCase = getVehicleCatalogUseCase;
        _getManagerVehicleUseCase = getManagerVehicleUseCase;
        _deleteVehicleUseCase = deleteVehicleUseCase;
    }

    // Catalog models not yet added to the fleet. Manager-only through the
    // class-level [Authorize(Roles = "Manager")].
    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<VehicleCatalogResponse>>> GetCatalog(CancellationToken cancellationToken)
    {
        var items = await _getVehicleCatalogUseCase.ExecuteAsync(cancellationToken);
        return Ok(items);
    }

    // 201 Created carries a Location header pointing at the created Vehicle
    // (no GET-by-id endpoint exists on this controller to route through, so
    // the URI is built directly -- the response body/status code are
    // otherwise unchanged).
    [HttpPost]
    public async Task<ActionResult<ManagerVehicleResponse>> Create([FromBody] CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await _createVehicleUseCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtRoute(GetByIdRouteName, new { id = vehicle.Id }, vehicle);
    }

    // The resource Create's Location header points to.
    [HttpGet("{id:int}", Name = GetByIdRouteName)]
    public async Task<ActionResult<ManagerVehicleResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var vehicle = await _getManagerVehicleUseCase.ExecuteAsync(id, cancellationToken);
        return Ok(vehicle);
    }

    // Deletes a never-used Available vehicle: 204; 404 if absent; 409 (bilingual
    // code) if it is not Available, has assignment/flight history, or changed
    // concurrently. Vehicles with history are retired instead, never deleted.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        await _deleteVehicleUseCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
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
