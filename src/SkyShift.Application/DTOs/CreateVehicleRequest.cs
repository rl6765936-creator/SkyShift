using System.ComponentModel.DataAnnotations;

namespace SkyShift.Application.DTOs;

public class CreateVehicleRequest
{
    public bool IsPersonalExperimental { get; set; }

    [StringLength(200, MinimumLength = 1)]
    [RegularExpression(@".*\S.*", ErrorMessage = "Model name cannot be blank.")]
    public string? ModelName { get; set; }

    [Range(1, 2000)]
    public int? MaxSpeedKmh { get; set; }

    [Range(1, 100)]
    public int? Seats { get; set; }

    [StringLength(2048, MinimumLength = 1)]
    [RegularExpression(@"^(https?://[^\s\\]+|/(?!/)[^\s\\]*)$", ErrorMessage = "Image URL must be an HTTP(S) URL or a root-relative asset path.")]
    public string? ImageUrl { get; set; }
}
