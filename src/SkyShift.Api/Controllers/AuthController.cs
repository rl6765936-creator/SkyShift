using Microsoft.AspNetCore.Mvc;
using SkyShift.Api.Authentication;
using SkyShift.Api.Models;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.LoginUser;
using SkyShift.Application.UseCases.RegisterUser;

namespace SkyShift.Api.Controllers;

/// <summary>
/// Authentication endpoints: Register and Login. Thin presentation code that calls the use cases, wraps
/// the result in a JWT from JwtTokenService and returns the HTTP response.
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
    /// Registers a new user and returns a JWT immediately, with a Location header for GET
    /// /api/users/{id}. A duplicate email throws EmailAlreadyRegisteredException, which
    /// SkyShiftExceptionHandler maps to 409.
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
            Gender = registerResult.Gender,
            Id = registerResult.Id,
            Name = registerResult.Name,
            Email = registerResult.Email,
            Token = token
        };

        return CreatedAtRoute(UsersController.GetByIdRouteName, new { id = registerResult.Id }, response);
    }

    /// <summary>
    /// Authenticates a user and returns a JWT. Invalid credentials surface
    /// as InvalidCredentialsException, which the global
    /// SkyShiftExceptionHandler maps to 401 with code invalid_credentials
    /// (and bilingual messages) -- no local try/catch is needed here.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginApiResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var loginResult = await _loginUserUseCase.ExecuteAsync(request, cancellationToken);

        var token = _jwtTokenService.GenerateToken(
            loginResult.Id,
            loginResult.Email,
            loginResult.Name,
            loginResult.Role.ToString());

        var response = new LoginApiResponse
        {
            Gender = loginResult.Gender,
            Id = loginResult.Id,
            Name = loginResult.Name,
            Email = loginResult.Email,
            Token = token
        };

        return Ok(response);
    }
}
