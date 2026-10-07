namespace SkyShift.Domain.Exceptions;

public sealed class DuplicateFlightException : Exception
{
    public DuplicateFlightException() : base("A flight is already recorded for this user at this date and time.") { }
}
