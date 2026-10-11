namespace SkyShift.Application.Abstractions;

/// <summary>
/// Persists all changes made during the current use case as a single unit. Transaction mechanics
/// (isolation level, begin/commit, EF Core) stay inside the Infrastructure implementation.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
