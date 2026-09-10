using System.ComponentModel.DataAnnotations;

namespace SkyShift.Application.DTOs;

public class RegisterUserRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "AverageDailyTrips must be a positive number.")]
    public int AverageDailyTrips { get; set; }
}
