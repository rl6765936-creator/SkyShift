namespace SkyShift.Domain.Exceptions;

public sealed class InvalidFlightDateException : Exception
{
    public string Code { get; }

    public InvalidFlightDateException(string code, string message) : base(message)
    {
        Code = code;
    }
}
