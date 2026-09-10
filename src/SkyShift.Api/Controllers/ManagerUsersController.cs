using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetAllUsersActivity;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Manager user/activity monitoring. Exposes raw per-user facts only --
/// see UserActivityResponse and GetAllUsersActivityUseCase for why no
/// "Active User" boolean is invented here.
/// </summary>
[ApiController]
[Route("api/manager/users")]
[Authorize(Roles = "Manager")]
public class ManagerUsersController : ControllerBase
{
    private readonly GetAllUsersActivityUseCase _getAllUsersActivityUseCase;

    public ManagerUsersController(GetAllUsersActivityUseCase getAllUsersActivityUseCase)
    {
        _getAllUsersActivityUseCase = getAllUsersActivityUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserActivityResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _getAllUsersActivityUseCase.ExecuteAsync(cancellationToken);
        return Ok(users);
    }
}
