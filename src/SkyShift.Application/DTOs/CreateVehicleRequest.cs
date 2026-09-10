namespace SkyShift.Application.DTOs;

// No Status and no CreatedAt fields, by design: a newly created Vehicle
// always starts Available, and CreatedAt is always server-generated
// (DateTime.UtcNow) inside CreateVehicleUseCase. Neither can be supplied
// by the client because neither exists on this DTO to bind to.
//
// No Data Annotations: bool has no invalid state to validate against.
public class CreateVehicleRequest
{
    public bool IsPersonalExperimental { get; set; }
}
