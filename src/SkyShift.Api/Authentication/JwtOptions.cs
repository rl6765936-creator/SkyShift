using System.ComponentModel.DataAnnotations;

namespace SkyShift.Api.Authentication;

/// <summary>
/// Configuration bound from the "Jwt" section; SigningKey comes only from configuration (User Secrets
/// or environment variables), never from code. The [Required]/[Range] rules are validated at startup,
/// so a missing signing key fails fast instead of on the first token operation.
/// </summary>
public class JwtOptions
{
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SigningKey { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ExpirationMinutes { get; set; }
}
