using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SkyShift.Domain.Enums;

namespace SkyShift.Application.DTOs;

public class ChangeVehicleStatusRequest
{
    // Accepted as its name ("Available", "Retired", ...) as well as the
    // underlying int (JsonStringEnumConverter accepts both on read),
    // scoped to this Manager-only DTO -- no existing request contract is
    // touched. EnumDataType rejects any value outside the defined
    // VehicleStatus members (e.g. an out-of-range int) with a clear 400
    // instead of silently accepting it.
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [EnumDataType(typeof(VehicleStatus), ErrorMessage = "TargetStatus must be one of: Available, InUse, Maintenance, Unavailable, Retired.")]
    public VehicleStatus TargetStatus { get; set; }
}
