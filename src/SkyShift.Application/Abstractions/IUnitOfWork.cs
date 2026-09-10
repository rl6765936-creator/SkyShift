namespace SkyShift.Application.Abstractions;

/// <summary>
/// Persists all changes made during the current Use Case as a single unit.
/// Deliberately minimal: Application only needs "commit what I did," never
/// transaction mechanics (isolation level, explicit Begin/Commit, DbContext,
/// EF Core) -- those stay entirely inside Infrastructure's implementation.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
