namespace SkyShift.Domain.Exceptions;

/// <summary>
/// The email is already registered; SkyShiftExceptionHandler maps it to 409 with code
/// duplicate_email. It derives from InvalidOperationException for compatibility with existing
/// callers.
/// </summary>
public sealed class EmailAlreadyRegisteredException : InvalidOperationException
{
    public EmailAlreadyRegisteredException(string email)
        : base($"Email '{email}' is already registered.")
    {
    }
}
