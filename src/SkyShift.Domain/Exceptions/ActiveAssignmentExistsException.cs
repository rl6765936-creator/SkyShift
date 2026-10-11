namespace SkyShift.Domain.Exceptions;

/// <summary>
/// Expected conflict: a User tried to select a Vehicle while already holding an
/// active VehicleAssignment (one active assignment per User). Maps to 409.
/// </summary>
public sealed class ActiveAssignmentExistsException : DomainException
{
    public ActiveAssignmentExistsException()
        : base("User already has an active vehicle assignment.")
    {
    }
}
