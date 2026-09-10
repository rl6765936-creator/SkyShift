using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

// Intentionally holds no Token field — token issuance is a separate,
// not-yet-decided Authentication topic (see PROJECT_DECISIONS.md).
public class LoginResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Added for the Manager MVP: SkyShift.Api needs the User's Role to
    // include it as a JWT claim. Not previously present on this DTO.
    public UserRole Role { get; set; }
}
