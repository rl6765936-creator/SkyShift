using SkyShift.Domain.Enums;

namespace SkyShift.Domain.Entities;

/// <summary>
/// A registered participant in the SkyShift initiative. It stores only a password hash, never a
/// plain-text password, and does not know which algorithm produced it (IPasswordHasher).
/// </summary>
public class User
{
    private const int DaysInMonth = 30;

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public int AverageDailyTrips { get; private set; }
    public string PasswordHash { get; private set; }
    public UserGender? Gender { get; private set; }

    /// <summary>
    /// Always User for any User created through User.Create -- there is no
    /// public code path that produces a Manager. Promotion to Manager only
    /// ever happens via PromoteToManager() below, and the only caller of
    /// that method is the internal startup demo-user seeder -- never a
    /// Controller, never anything reachable from a client request.
    /// </summary>
    public UserRole Role { get; private set; }

    private readonly List<VehicleAssignment> _vehicleAssignments = new();

    /// <summary>
    /// EF Core navigation for the many-to-many relationship between User and Vehicle, realized
    /// through VehicleAssignment. It is read-only and populated by EF Core when included;
    /// assignments are created only through SelectVehicleUseCase.
    /// </summary>
    public IReadOnlyCollection<VehicleAssignment> VehicleAssignments => _vehicleAssignments.AsReadOnly();

    private User(string name, string email, int averageDailyTrips, string passwordHash)
    {
        if (averageDailyTrips < 0)
            throw new ArgumentOutOfRangeException(nameof(averageDailyTrips), "Average daily trips cannot be negative.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

        Name = name;
        Email = email;
        AverageDailyTrips = averageDailyTrips;
        PasswordHash = passwordHash;
        Role = UserRole.User;
    }

    public static User Create(string name, string email, int averageDailyTrips, string passwordHash, UserGender? gender = null)
    {
        if (gender.HasValue && !Enum.IsDefined(gender.Value))
            throw new ArgumentOutOfRangeException(nameof(gender));

        return new User(name, email, averageDailyTrips, passwordHash) { Gender = gender };
    }

    /// <summary>
    /// Promotes this user to Manager. It is called only by the startup demo-user seeder
    /// (SeedDemoUsersUseCase), and no API request can set a role.
    /// </summary>
    public void PromoteToManager()
    {
        Role = UserRole.Manager;
    }

    /// <summary>
    /// Business rule: the user's monthly trip target is derived from their
    /// declared average daily trips (e.g. 4/day -> 120/month).
    /// </summary>
    public int CalculateExpectedMonthlyTrips()
    {
        return AverageDailyTrips * DaysInMonth;
    }
}
