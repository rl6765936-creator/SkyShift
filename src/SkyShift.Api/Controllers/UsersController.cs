using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetUserProfile;
using SkyShift.Domain.Enums;

namespace SkyShift.Api.Controllers;

/// <summary>
/// User resource addressed by POST /api/auth/register's Location header.
/// Authenticated callers may read their own profile; Managers may read any
/// (rule enforced in GetUserProfileUseCase; others get 404).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    public const string GetByIdRouteName = "GetUserById";

    private readonly GetUserProfileUseCase _getUserProfileUseCase;

    public UsersController(GetUserProfileUseCase getUserProfileUseCase)
    {
        _getUserProfileUseCase = getUserProfileUseCase;
    }

    [HttpGet("{id:int}", Name = GetByIdRouteName)]
    public async Task<ActionResult<RegisterUserResponse>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var callerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _getUserProfileUseCase.ExecuteAsync(id, callerId, User.IsInRole(nameof(UserRole.Manager)), cancellationToken);
        return Ok(profile);
    }
}
