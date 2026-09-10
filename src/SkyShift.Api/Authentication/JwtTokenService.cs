using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SkyShift.Api.Authentication;

/// <summary>
/// API-only JWT generation service. Approved decision: JWT ownership belongs
/// entirely to SkyShift.Api -- there is no ITokenGenerator in Application,
/// and Application/Domain remain unaware that JWT (or any token concept)
/// exists. This class contains no business logic and does not access the
/// database, repositories, or any Application Use Case -- it only turns an
/// already-authenticated identity (UserId, Email, Name, Role, obtained by
/// the caller from a successful RegisterUserUseCase/LoginUserUseCase call)
/// into a signed token string.
///
/// Manager MVP change: this is the one, explicitly approved amendment to
/// the prior "exactly 3 claims, no Roles" decision. A 4th claim,
/// ClaimTypes.Role, is added below so that [Authorize(Roles = "Manager")]
/// works using ASP.NET Core's default RoleClaimType. Every other setting
/// (signing algorithm, Issuer/Audience, ClockSkew, JWT ownership by Api)
/// is unchanged.
/// </summary>
public class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateToken(int userId, string email, string name, string role)
    {
        // The three original approved application-level identity claims,
        // plus the one approved addition: ClaimTypes.Role, so that
        // [Authorize(Roles = "Manager")] can evaluate it via ASP.NET Core's
        // default RoleClaimType. Still no permissions, no other claims.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, role)
        };

        // Symmetric key + HMAC-SHA256, per the approved algorithm decision.
        // The key value itself comes only from configuration (User Secrets
        // in Development, Environment Variables/Secret Store in
        // Production) -- never a literal here.
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
