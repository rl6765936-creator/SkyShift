using SkyShift.Domain.Enums;
using System.Text.Json.Serialization;

namespace SkyShift.Application.DTOs;

public class RegisterUserResponse
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UserGender? Gender { get; set; }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Added for the Manager MVP: SkyShift.Api needs the User's Role to
    // include it as a JWT claim. Not previously present on this DTO.
    public UserRole Role { get; set; }
}
