namespace SkyShift.Domain.Exceptions;

/// <summary>The requested catalog item does not exist. Mapped to 404 with code catalog_item_not_found.</summary>
public sealed class CatalogItemNotFoundException : KeyNotFoundException
{
    public int CatalogItemId { get; }

    public CatalogItemNotFoundException(int catalogItemId)
        : base($"Catalog item {catalogItemId} was not found.")
    {
        CatalogItemId = catalogItemId;
    }
}
