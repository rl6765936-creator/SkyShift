using System.ComponentModel.DataAnnotations;

namespace SkyShift.Application.DTOs;

public class SelectVehicleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "VehicleId must be a positive integer.")]
    public int VehicleId { get; set; }
}
