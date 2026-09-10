namespace SkyShift.Domain.Enums;

/// <summary>
/// Distinguishes a normal initiative participant (User) from a System
/// Manager. User.Create always assigns User -- there is no public code path
/// that can create a Manager. The first Manager is promoted directly in the
/// database (a manual, one-time administrative action), never through the
/// API.
/// </summary>
public enum UserRole
{
    User,
    Manager
}
