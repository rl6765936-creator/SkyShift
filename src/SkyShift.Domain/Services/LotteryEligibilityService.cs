using SkyShift.Domain.Entities;

namespace SkyShift.Domain.Services;

/// <summary>
/// Domain Service (not a User method) because eligibility depends on data
/// owned by two separate Aggregates: User.AverageDailyTrips and the count of
/// that user's completed Flights. Keeping this outside User avoids coupling
/// the User aggregate to the Flight aggregate.
/// </summary>
public class LotteryEligibilityService
{
    private const int RequiredPercentageThreshold = 80;

    /// <summary>
    /// Business rule: a user is eligible for the lottery once at least
    /// RequiredPercentageThreshold percent of their expected monthly trips
    /// were completed as air trips (e.g. 96/120 = 80%).
    /// </summary>
    public bool IsEligible(User user, int completedFlights)
    {
        if (completedFlights < 0)
            throw new ArgumentOutOfRangeException(nameof(completedFlights), "Completed flights cannot be negative.");

        var expectedMonthlyTrips = user.CalculateExpectedMonthlyTrips();
        if (expectedMonthlyTrips <= 0)
            return false;

        // completedFlights / expectedMonthlyTrips >= threshold / 100,
        // cross-multiplied so the comparison needs no division at all.
        return completedFlights * 100 >= RequiredPercentageThreshold * expectedMonthlyTrips;
    }

    /// <summary>
    /// Business calculation: the percentage of expected monthly trips that were
    /// actually completed as air trips (e.g. 96/120 -> 80). Kept alongside
    /// IsEligible because it is the same underlying business computation,
    /// exposed here for display purposes (e.g. a dashboard) rather than as a
    /// threshold decision.
    /// </summary>
    public int CalculateCompletionPercentage(User user, int completedFlights)
    {
        if (completedFlights < 0)
            throw new ArgumentOutOfRangeException(nameof(completedFlights), "Completed flights cannot be negative.");

        var expectedMonthlyTrips = user.CalculateExpectedMonthlyTrips();
        if (expectedMonthlyTrips <= 0)
            return 0;

        return completedFlights * 100 / expectedMonthlyTrips;
    }
}
