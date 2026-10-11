using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.SeedDemoUsers;

/// <summary>
/// Creates the two documented demo accounts (one User, one Manager) at startup, and never seeds
/// vehicles, flights or assignments. It is idempotent: each account is looked up by email and
/// created only if missing, using User.Create, the existing IPasswordHasher and, for the Manager,
/// User.PromoteToManager(). The credentials are intentionally public demo values documented in the
/// README, not secrets.
/// </summary>
public class SeedDemoUsersUseCase
{
    public const string DemoUserEmail = "user@skyshift.demo";
    public const string DemoUserPassword = "User@12345";

    public const string DemoManagerEmail = "manager@skyshift.demo";
    public const string DemoManagerPassword = "Manager@12345";

    private const int DemoAverageDailyTrips = 4;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public SeedDemoUsersUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync()
    {
        var didWork = false;

        if (await _userRepository.GetByEmailAsync(DemoUserEmail) is null)
        {
            var passwordHash = _passwordHasher.Hash(DemoUserPassword);
            var demoUser = User.Create("Demo User", DemoUserEmail, DemoAverageDailyTrips, passwordHash);
            await _userRepository.AddAsync(demoUser);
            didWork = true;
        }

        if (await _userRepository.GetByEmailAsync(DemoManagerEmail) is null)
        {
            var passwordHash = _passwordHasher.Hash(DemoManagerPassword);
            var demoManager = User.Create("Demo Manager", DemoManagerEmail, DemoAverageDailyTrips, passwordHash);
            demoManager.PromoteToManager();
            await _userRepository.AddAsync(demoManager);
            didWork = true;
        }

        if (didWork)
            await _unitOfWork.SaveChangesAsync();
    }
}
