namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Expected conflict: a User tried to record a Flight without an active
/// VehicleAssignment. Maps to 409.
/// </summary>
public sealed class NoActiveAssignmentException : DomainException
{
    public NoActiveAssignmentException()
        : base("User has no active vehicle assignment.")
    {
    }
}
