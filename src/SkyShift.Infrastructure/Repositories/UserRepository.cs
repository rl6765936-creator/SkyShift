using Microsoft.EntityFrameworkCore;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;
using SkyShift.Infrastructure.Persistence;

namespace SkyShift.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SkyShiftDbContext _context;

    public UserRepository(SkyShiftDbContext context)
    {
        _context = context;
    }

    // Not part of the SelectVehicle critical path -- User lookups/inserts
    // have no relation to the VehicleAssignment concurrency race, so no
    // ambient transaction is opened here. A plain SaveChangesAsync() still
    // wraps this in EF Core's own implicit transaction when needed, and
    // Email uniqueness is enforced by a real DB unique index regardless.
    // Read-only: nothing in this codebase mutates a User returned from
    // either of these two and then relies on EF Core's change tracker to
    // persist it (IUserRepository has no UpdateAsync at all -- there is no
    // User-update flow). AsNoTracking() is safe here for the same reason
    // GetAllAsync below already uses it.
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    // Manager MVP addition. Plain listing query for Manager user/activity
    // monitoring and Dashboard aggregation.
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
