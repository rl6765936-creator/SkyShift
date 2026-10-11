namespace SkyShift.Domain.Exceptions;

/// <summary>
/// The catalog item is already linked to a fleet vehicle (whatever that
/// vehicle's current status). Raised by the application check and, under
/// concurrency, translated from the unique index IX_Vehicles_CatalogItemId.
/// Mapped to 409 with code catalog_item_in_use.
/// </summary>
public sealed class CatalogItemInUseException : Exception
{
    public CatalogItemInUseException()
        : base("This catalog model has already been added to the fleet. Choose another model.")
    {
    }
}
