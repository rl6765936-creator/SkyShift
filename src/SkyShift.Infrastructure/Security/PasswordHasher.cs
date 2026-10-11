using Microsoft.AspNetCore.Identity;
using SkyShift.Application.Abstractions;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Security;

/// <summary>
/// Wraps ASP.NET Core Identity's PBKDF2-based PasswordHasher&lt;TUser&gt; from
/// Microsoft.Extensions.Identity.Core. The Domain User is used only as the generic type argument;
/// the default implementation reads only the password, so no user instance is needed.
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
