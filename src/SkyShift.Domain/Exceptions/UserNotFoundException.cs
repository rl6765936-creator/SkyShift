namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Expected not-found result for a User id (also used when a caller may not see
/// another user's profile, so the response does not reveal whether it exists). Maps to 404.
/// </summary>
public sealed class UserNotFoundException : KeyNotFoundException
{
    public UserNotFoundException(int userId)
        : base($"User {userId} was not found.")
    {
    }
}
