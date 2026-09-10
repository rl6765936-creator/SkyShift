using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetFlightStats;

/// <summary>
/// Aggregated Manager flight statistics. Fetches the full, unfiltered
/// Flight set once via the same IFlightRepository.GetAllAsync used by
/// GetAllFlightsUseCase, and performs all grouping/aggregation here in
/// Application -- Infrastructure stays a plain data source, per the
/// "no unnecessary repository methods" constraint. ActivityTrend buckets by
/// CreatedAt.Date (the trusted server timestamp), never by Date.
/// </summary>
public class GetFlightStatsUseCase
{
    private readonly IFlightRepository _flightRepository;

    public GetFlightStatsUseCase(IFlightRepository flightRepository)
    {
        _flightRepository = flightRepository;
    }

    public async Task<FlightStatsResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var flights = await _flightRepository.GetAllAsync(null, null, null, null, cancellationToken);

        var flightsPerUser = flights
            .GroupBy(f => f.UserId)
            .Select(g => new UserFlightCount { UserId = g.Key, Count = g.Count() })
            .ToList();

        var flightsPerVehicle = flights
            .Where(f => f.VehicleId.HasValue)
            .GroupBy(f => f.VehicleId!.Value)
            .Select(g => new VehicleFlightCount { VehicleId = g.Key, Count = g.Count() })
            .ToList();

        var activityTrend = flights
            .GroupBy(f => f.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyFlightCount { Date = g.Key, Count = g.Count() })
            .ToList();

        return new FlightStatsResponse
        {
            TotalFlights = flights.Count,
            FlightsPerUser = flightsPerUser,
            FlightsPerVehicle = flightsPerVehicle,
            ActivityTrend = activityTrend
        };
    }
}
