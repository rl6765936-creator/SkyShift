using SkyShift.Domain.Enums;

namespace SkyShift.Domain.Entities;

/// <summary>
/// A registered participant in the "עולים לאוויר" initiative.
/// Holds only a PasswordHash — never a plain-text password, and has no
/// knowledge of which hashing algorithm produced it (that is an Application/
/// Infrastructure concern, via IPasswordHasher).
/// </summary>
public class User
{
    private const int DaysInMonth = 30;

    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public int AverageDailyTrips { get; private set; }
    public string PasswordHash { get; private set; }

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
    /// EF Core navigation side of the User &lt;-&gt; Vehicle many-to-many
    /// relationship, realized through VehicleAssignment (see
    /// VehicleAssignment.User/Vehicle and VehicleAssignmentConfiguration) --
    /// a User can hold many VehicleAssignments over time, each to a
    /// different Vehicle, and a Vehicle can likewise be assigned to many
    /// different Users over time, so the pairing itself is many-to-many even
    /// though only one assignment per User is ever active at once.
    /// Read-only here: populated by EF Core via the private backing field
    /// when eager-loaded, never mutated directly -- assignments are still
    /// created exclusively through SelectVehicleUseCase, unchanged.
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

    public static User Create(string name, string email, int averageDailyTrips, string passwordHash)
    {
        return new User(name, email, averageDailyTrips, passwordHash);
    }

    /// <summary>
    /// Promotes this User to Manager. Exists solely to support deterministic
    /// demo-user seeding at startup (see Application's SeedDemoUsersUseCase) --
    /// it is never invoked from any Controller or public API path, and no
    /// request DTO anywhere carries a Role for a client to set. This
    /// preserves the original decision that Manager cannot be created
    /// through the public API: the only caller of this method is the
    /// internal startup seeder, which runs outside any HTTP request.
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
