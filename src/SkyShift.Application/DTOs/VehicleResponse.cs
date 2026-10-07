using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

public class VehicleResponse
{
    public string? ModelName { get; set; }
    public int? MaxSpeedKmh { get; set; }
    public int? Seats { get; set; }
    public string? ImageUrl { get; set; }

    public int Id { get; set; }
    public VehicleStatus Status { get; set; }
    public bool IsPersonalExperimental { get; set; }
}
