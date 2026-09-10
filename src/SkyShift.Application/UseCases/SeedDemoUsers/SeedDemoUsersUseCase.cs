using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.UseCases.SeedDemoUsers;

/// <summary>
/// Deterministic startup seeding of exactly two demo accounts -- one User,
/// one Manager -- so the system is runnable and demonstrable immediately
/// after database creation, per the course project's submission
/// requirements. Deliberately narrow in scope: no Vehicle, Flight, or
/// VehicleAssignment is ever seeded here (that stays the Manager's job via
/// the existing API, per the earlier approved decision).
///
/// Idempotent by construction: each account is looked up by email first
/// (the same IUserRepository.GetByEmailAsync already used by
/// RegisterUserUseCase's duplicate check) and only created if missing, so
/// running this on every application startup never creates duplicates and
/// never touches an already-seeded row.
///
/// The Manager account is produced by creating a normal User through the
/// existing User.Create factory (so it goes through the exact same
/// validation every other User does) and then calling the new, narrowly-
/// scoped User.PromoteToManager() -- the only caller of that method in the
/// whole solution. This process runs from Program.cs at startup, never from
/// a Controller, so the public API surface for creating/promoting a Manager
/// is unchanged: still nothing.
///
/// Credentials are fixed, documented demo credentials (see the server
/// README), not secrets -- unlike the JWT signing key or the DB connection
/// string, they are meant to be public so a grader can log in immediately.
/// The existing IPasswordHasher is used to hash them, exactly as
/// RegisterUserUseCase does for a real registration.
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
