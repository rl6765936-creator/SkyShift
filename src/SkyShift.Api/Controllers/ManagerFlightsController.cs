using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetAllFlights;
using SkyShift.Application.UseCases.GetFlightStats;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Manager flight monitoring: a filterable, database-paginated flight list and aggregated statistics
/// over the full filtered set. The from/to filters use the business Date field, while the activity
/// trend is bucketed by the server CreatedAt timestamp.
/// </summary>
[ApiController]
[Route("api/manager/flights")]
[Authorize(Roles = "Manager")]
public class ManagerFlightsController : ControllerBase
{
    private readonly GetAllFlightsUseCase _getAllFlightsUseCase;
    private readonly GetFlightStatsUseCase _getFlightStatsUseCase;

    public ManagerFlightsController(
        GetAllFlightsUseCase getAllFlightsUseCase,
        GetFlightStatsUseCase getFlightStatsUseCase)
    {
        _getAllFlightsUseCase = getAllFlightsUseCase;
        _getFlightStatsUseCase = getFlightStatsUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<FlightResponse>>> GetAll(
        [FromQuery] int? userId,
        [FromQuery] int? vehicleId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var flights = await _getAllFlightsUseCase.ExecuteAsync(userId, vehicleId, from, to, page, pageSize, cancellationToken);
        return Ok(flights);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<FlightStatsResponse>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _getFlightStatsUseCase.ExecuteAsync(cancellationToken);
        return Ok(stats);
    }
}
