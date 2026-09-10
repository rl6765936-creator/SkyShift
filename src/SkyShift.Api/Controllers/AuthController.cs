using Microsoft.AspNetCore.Mvc;
using SkyShift.Api.Authentication;
using SkyShift.Api.Models;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.LoginUser;
using SkyShift.Application.UseCases.RegisterUser;

namespace SkyShift.Api.Controllers;

/// <summary>
/// MVP authentication endpoints: Register and Login. Thin presentation
/// code only -- it calls Application Use Cases, turns the result into a
/// signed JWT via the existing API-only JwtTokenService, and maps the
/// outcome to an HTTP response. No business logic, no password hashing,
/// no repository/DbContext access, and no JWT generation logic of its own.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RegisterUserUseCase _registerUserUseCase;
    private readonly LoginUserUseCase _loginUserUseCase;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        RegisterUserUseCase registerUserUseCase,
        LoginUserUseCase loginUserUseCase,
        JwtTokenService jwtTokenService)
    {
        _registerUserUseCase = registerUserUseCase;
        _loginUserUseCase = loginUserUseCase;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Registers a new user and returns a JWT immediately -- the user is
    /// never required to call Login separately just to obtain their first
    /// token. Duplicate email is not caught here: RegisterUserUseCase
    /// throws InvalidOperationException, which is left to propagate to the
    /// global SkyShiftExceptionHandler, which maps it to 409 Conflict.
    ///
    /// 201 Created carries a Location header pointing at the created User
    /// (no GET-by-id endpoint exists to route through, so the URI is built
    /// directly -- the response body/status code are otherwise unchanged).
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<LoginApiResponse>> Register([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var registerResult = await _registerUserUseCase.ExecuteAsync(request, cancellationToken);

        var token = _jwtTokenService.GenerateToken(
            registerResult.Id,
            registerResult.Email,
            registerResult.Name,
            registerResult.Role.ToString());

        var response = new LoginApiResponse
        {
            Id = registerResult.Id,
            Name = registerResult.Name,
            Email = registerResult.Email,
            Token = token
        };

        return Created($"/api/users/{registerResult.Id}", response);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT. Invalid credentials are
    /// represented by LoginUserUseCase throwing InvalidOperationException --
    /// that same exception type maps to 409 everywhere else in the system,
    /// so it is caught locally, here only, and translated to 401
    /// Unauthorized instead of being left to reach the global exception
    /// handler. Any other, unexpected exception is not caught here and
    /// still reaches the global handler normally.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginApiResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        LoginResponse loginResult;
        try
        {
            loginResult = await _loginUserUseCase.ExecuteAsync(request, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Unauthorized();
        }

        var token = _jwtTokenService.GenerateToken(
            loginResult.Id,
            loginResult.Email,
            loginResult.Name,
            loginResult.Role.ToString());

        var response = new LoginApiResponse
        {
            Id = loginResult.Id,
            Name = loginResult.Name,
            Email = loginResult.Email,
            Token = token
        };

        return Ok(response);
    }
}
