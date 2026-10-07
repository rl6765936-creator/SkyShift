using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.RegisterUser;

/// <summary>
/// Registers a new User. Email-uniqueness is checked here (not in Domain)
/// because a single User instance cannot know about other Users — it
/// requires a Repository query. All other invariants (AverageDailyTrips,
/// PasswordHash) are enforced by User.Create itself.
/// </summary>
public class RegisterUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RegisterUserUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<RegisterUserResponse> ExecuteAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
            throw new InvalidOperationException($"Email '{request.Email}' is already registered.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Name, request.Email, request.AverageDailyTrips, passwordHash, request.Gender);

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<RegisterUserResponse>(user);
    }
}
