using Microsoft.AspNetCore.Identity;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PasswordHasher&lt;TUser&gt; (PBKDF2-based),
/// provided by the approved Microsoft.Extensions.Identity.Core package.
/// TUser is SkyShift's own Domain User only to satisfy the generic API's
/// shape -- the default implementation does not read or depend on any of
/// User's properties, only the password string passed explicitly, so
/// passing no real instance is safe here.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _identityHasher = new();

    public string Hash(string plainPassword)
    {
        return _identityHasher.HashPassword(default!, plainPassword);
    }

    public bool Verify(string plainPassword, string passwordHash)
    {
        var result = _identityHasher.VerifyHashedPassword(default!, passwordHash, plainPassword);
        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
