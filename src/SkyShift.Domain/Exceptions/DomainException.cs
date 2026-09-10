namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Common base type for exceptions that represent a violated business rule or
/// invariant inside the Domain itself. Its value is internal to Domain: it
/// gives business-rule violations a distinct type identity, separate from
/// ordinary technical exceptions (e.g. ArgumentException on invalid input),
/// so Domain's own vocabulary of "this is a business-rule failure" is
/// expressed in the type system rather than left implicit. This type carries
/// no dependency on, and no awareness of, any outer layer.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
