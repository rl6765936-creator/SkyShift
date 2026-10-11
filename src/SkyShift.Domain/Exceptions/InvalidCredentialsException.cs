namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Login failed. Deliberately does not say whether the email or the password
/// was wrong. Derives from InvalidOperationException to keep the original
/// LoginUserUseCase contract; the handler maps it to 401 invalid_credentials.
/// </summary>
public sealed class InvalidCredentialsException : InvalidOperationException
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}
