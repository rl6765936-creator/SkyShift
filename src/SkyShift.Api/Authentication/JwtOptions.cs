using System.ComponentModel.DataAnnotations;

namespace SkyShift.Api.Authentication;

/// <summary>
/// Plain configuration POCO bound to the "Jwt" configuration section
/// (appsettings.json for non-secret defaults, User Secrets in Development
/// and Environment Variables/Secret Store in Production for SigningKey).
/// Holds no logic and no hard-coded secret -- SigningKey is only ever
/// populated by configuration binding, never assigned a literal value here.
///
/// The [Required]/[Range] attributes below are enforced at startup via
/// Program.cs's AddOptions&lt;JwtOptions&gt;().ValidateDataAnnotations().ValidateOnStart(),
/// mirroring the existing fail-fast check already done for
/// ConnectionStrings:SkyShiftDb -- a missing/blank Jwt:SigningKey (the
/// typical case before User Secrets are configured) now fails immediately
/// with a clear message instead of only surfacing later, cryptically, on
/// the first token generation or validation attempt.
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
