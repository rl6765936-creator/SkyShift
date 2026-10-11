using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IVehicleCatalogRepository
{
    /// <summary>
    /// Catalog items not yet linked to any fleet vehicle, whatever that
    /// vehicle's status (Available, InUse, Maintenance, Unavailable or Retired).
    /// </summary>
    Task<IReadOnlyList<VehicleCatalogItem>> GetUnusedAsync(CancellationToken cancellationToken = default);

    Task<VehicleCatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Early, friendly check only. The unique index on Vehicles.CatalogItemId
    /// is the final guard under concurrency.
    /// </summary>
    Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default);
}
