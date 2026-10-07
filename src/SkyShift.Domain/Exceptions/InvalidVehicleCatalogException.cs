namespace SkyShift.Domain.Exceptions;

public sealed class InvalidVehicleCatalogException : Exception
{
    public InvalidVehicleCatalogException(string message) : base(message) { }
}
