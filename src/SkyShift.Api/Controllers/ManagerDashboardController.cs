using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetManagerDashboard;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Manager-wide Dashboard: fleet/user/flight/assignment totals, Vehicle
/// utilization, and the two separate Participation numbers (Engagement
/// Rate, Compliance Rate). All computation lives in
/// GetManagerDashboardUseCase -- this controller only calls it and returns
/// the result, exactly like the existing per-user DashboardController.
/// </summary>
[ApiController]
[Route("api/manager/dashboard")]
[Authorize(Roles = "Manager")]
public class ManagerDashboardController : ControllerBase
{
    private readonly GetManagerDashboardUseCase _getManagerDashboardUseCase;

    public ManagerDashboardController(GetManagerDashboardUseCase getManagerDashboardUseCase)
    {
        _getManagerDashboardUseCase = getManagerDashboardUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<ManagerDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _getManagerDashboardUseCase.ExecuteAsync(cancellationToken);
        return Ok(dashboard);
    }
}
