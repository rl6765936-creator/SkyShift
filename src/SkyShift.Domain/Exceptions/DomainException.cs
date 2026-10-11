namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Base type for exceptions that represent a violated business rule or invariant. It gives
/// business-rule failures a distinct type from technical exceptions such as ArgumentException, and
/// has no dependency on outer layers.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
