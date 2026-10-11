using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Api.Models;

/// <summary>
/// API-only response for successful registration and login: the JWT Token alongside the basic identity
/// fields. It belongs to SkyShift.Api only, so Application's RegisterUserResponse and LoginResponse
/// stay unaware of tokens.
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
