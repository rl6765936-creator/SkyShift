using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Api.Models;

/// <summary>
/// API-only response for successful registration and login. Carries the
/// JWT Token alongside the basic identity fields. This type belongs
/// exclusively to SkyShift.Api -- Application's RegisterUserResponse and
/// LoginResponse remain unchanged and unaware that tokens exist.
/// </summary>
public class LoginApiResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserGender? Gender { get; init; }

    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}
