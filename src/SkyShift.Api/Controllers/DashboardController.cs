using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetUserDashboard;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Returns the authenticated user's dashboard. Thin presentation code: it reads the user id from
/// the JWT and calls GetUserDashboardUseCase, which computes every value. Exceptions propagate to
/// SkyShiftExceptionHandler.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly GetUserDashboardUseCase _getUserDashboardUseCase;

    public DashboardController(GetUserDashboardUseCase getUserDashboardUseCase)
    {
        _getUserDashboardUseCase = getUserDashboardUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dashboard = await _getUserDashboardUseCase.ExecuteAsync(userId, cancellationToken);
        return Ok(dashboard);
    }
}
