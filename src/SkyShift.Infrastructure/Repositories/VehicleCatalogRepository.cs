using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Infrastructure.Persistence;

namespace SkyShift.Infrastructure.Repositories;

/// <summary>
/// Read-only catalog queries that never open the critical-path transaction. "Used" means any Vehicle
/// row references the item, regardless of that vehicle's status.
/// </summary>
public class VehicleCatalogRepository : IVehicleCatalogRepository
{
    private readonly SkyShiftDbContext _context;

    public VehicleCatalogRepository(SkyShiftDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<VehicleCatalogItem>> GetUnusedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.VehicleCatalogItems
            .AsNoTracking()
            .Where(item => !_context.Vehicles.Any(v => v.CatalogItemId == item.Id))
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<VehicleCatalogItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.VehicleCatalogItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> IsInUseAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Vehicles
            .AsNoTracking()
            .AnyAsync(v => v.CatalogItemId == id, cancellationToken);
    }
}
