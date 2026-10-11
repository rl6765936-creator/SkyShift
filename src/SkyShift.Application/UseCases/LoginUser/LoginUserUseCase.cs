using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.LoginUser;

/// <summary>
/// Verifies credentials and returns the authenticated user's identity, or throws
/// InvalidCredentialsException. It issues no token; the Api's JwtTokenService creates the JWT from
/// this result.
/// </summary>
public class LoginUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IMapper _mapper;

    public LoginUserUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher, IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return _mapper.Map<LoginResponse>(user);
    }
}
