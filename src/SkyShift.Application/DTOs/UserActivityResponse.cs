namespace SkyShift.Application.DTOs;

// Deliberately exposes raw facts only -- no "IsActive" boolean. There is
// no approved business definition of "Active User" yet (see
// PROJECT_DECISIONS.md discussion); inventing one here would silently
// bake in an unapproved definition.
public class UserActivityResponse
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalFlights { get; set; }
    public bool HasActiveAssignment { get; set; }

    // Based on Flight.CreatedAt (trusted server timestamp), not Flight.Date.
    public DateTime? LastFlightCreatedAt { get; set; }
}
