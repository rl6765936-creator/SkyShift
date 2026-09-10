using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

public class VehicleResponse
{
    public int Id { get; set; }
    public VehicleStatus Status { get; set; }
    public bool IsPersonalExperimental { get; set; }
}
