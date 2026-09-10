using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IVehicleRepository
{
    // Existing members -- part of the SelectVehicle critical path, unchanged.
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    // Manager MVP additions -- deliberately NOT part of the SelectVehicle
    // critical path (no ambient Serializable transaction): Manager fleet
    // management is unrelated to the SelectVehicle concurrency race.
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Vehicle?> GetByIdForManagementAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
