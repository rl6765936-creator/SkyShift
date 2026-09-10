namespace SkyShift.Application.DTOs;

public class FlightStatsResponse
{
    public int TotalFlights { get; set; }
    public IReadOnlyList<UserFlightCount> FlightsPerUser { get; set; } = new List<UserFlightCount>();
    public IReadOnlyList<VehicleFlightCount> FlightsPerVehicle { get; set; } = new List<VehicleFlightCount>();

    // Activity trend, bucketed by day, based on Flight.CreatedAt (trusted
    // server timestamp) -- not Flight.Date. See DailyFlightCount.
    public IReadOnlyList<DailyFlightCount> ActivityTrend { get; set; } = new List<DailyFlightCount>();
}
