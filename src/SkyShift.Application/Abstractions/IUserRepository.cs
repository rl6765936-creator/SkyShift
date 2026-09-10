using SkyShift.Domain.Entities;

namespace SkyShift.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manager MVP addition. Returns every registered User, for Manager
    /// user/activity monitoring and Dashboard aggregation.
    /// </summary>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default);
}
