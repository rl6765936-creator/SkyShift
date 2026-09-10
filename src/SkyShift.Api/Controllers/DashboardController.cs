using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetUserDashboard;

namespace SkyShift.Api.Controllers;

/// <summary>
/// MVP dashboard endpoint. Thin presentation code only -- it extracts the
/// authenticated UserId from the JWT claim and calls
/// GetUserDashboardUseCase, which alone computes ExpectedMonthlyTrips,
/// CompletedFlights, Percentage, IsLotteryEligible, and CurrentVehicle. No
/// business logic, no repository/DbContext access, and no exception
/// handling of its own: KeyNotFoundException is left to propagate to the
/// existing global SkyShiftExceptionHandler.
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
