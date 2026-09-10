namespace SkyShift.Application.DTOs;

public class VehicleStatusBreakdown
{
    public int Available { get; set; }
    public int InUse { get; set; }
    public int Maintenance { get; set; }
    public int Unavailable { get; set; }
    public int Retired { get; set; }
}
