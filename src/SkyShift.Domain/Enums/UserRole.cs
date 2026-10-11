namespace SkyShift.Domain.Enums;

/// <summary>
/// Distinguishes a normal participant (User) from a Manager. User.Create always assigns User, and
/// no API request can create a Manager. The demo Manager is created at startup by
/// SeedDemoUsersUseCase through User.PromoteToManager().
/// </summary>
public enum UserRole
{
    User,
    Manager
}
