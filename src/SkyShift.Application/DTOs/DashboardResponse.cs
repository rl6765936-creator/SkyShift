namespace SkyShift.Application.DTOs;

public class DashboardResponse
{
    public int ExpectedMonthlyTrips { get; set; }
    public int CompletedFlights { get; set; }
    public int Percentage { get; set; }
    public bool IsLotteryEligible { get; set; }
    public VehicleResponse? CurrentVehicle { get; set; }
}
