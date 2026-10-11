using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Exceptions;

namespace SkyShift.Application.UseCases.GetUserProfile;

/// <summary>
/// Returns a single user's profile (the target of the registration Location header) as
/// RegisterUserResponse, never including the password hash. Users may read only their own profile,
/// while Managers may read any. Another user's id is answered like a missing id (404), so existing
/// ids are not revealed.
/// </summary>
public class GetUserProfileUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetUserProfileUseCase(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<RegisterUserResponse> ExecuteAsync(int requestedUserId, int callerUserId, bool callerIsManager, CancellationToken cancellationToken = default)
    {
        if (requestedUserId != callerUserId && !callerIsManager)
            throw new UserNotFoundException(requestedUserId);

        var user = await _userRepository.GetByIdAsync(requestedUserId, cancellationToken)
            ?? throw new UserNotFoundException(requestedUserId);
        return _mapper.Map<RegisterUserResponse>(user);
    }
}
