using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IVehicleRepository
{
    // Existing members -- part of the SelectVehicle critical path, unchanged.
    Task<Vehicle?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    // Display-only lookup: untracked and outside the SelectVehicle transaction.
    // Never use the result for an update -- writes go through GetByIdAsync.
    Task<Vehicle?> GetByIdForReadAsync(int id, CancellationToken cancellationToken = default);

    // True when any VehicleAssignment or Flight references the vehicle. Runs inside
    // the same Serializable transaction as GetByIdAsync, for the delete check.
    Task<bool> HasHistoryAsync(int id, CancellationToken cancellationToken = default);

    // Marks a vehicle loaded with GetByIdAsync for deletion (same transaction; the
    // RowVersion token makes the DELETE fail if the row changed since it was read).
    Task DeleteAsync(Vehicle vehicle, CancellationToken cancellationToken = default);

    // Manager MVP additions -- deliberately NOT part of the SelectVehicle
    // critical path (no ambient Serializable transaction): Manager fleet
    // management is unrelated to the SelectVehicle concurrency race.
    Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vehicle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Vehicle?> GetByIdForManagementAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Vehicle vehicle, CancellationToken cancellationToken = default);
}
