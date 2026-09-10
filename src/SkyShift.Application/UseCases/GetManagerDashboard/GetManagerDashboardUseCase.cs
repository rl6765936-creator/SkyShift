using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Services;

namespace SkyShift.Application.UseCases.GetManagerDashboard;

/// <summary>
/// Aggregates the Manager MVP dashboard. Mirrors GetUserDashboardUseCase's
/// principle: Application fetches data and assembles the response, all
/// business calculation stays in Domain (LotteryEligibilityService) or is a
/// simple, explicitly-approved formula documented on ManagerDashboardResponse.
///
/// Utilization = InUse / (Total - Retired), Retired excluded from the
/// denominator (0 when that denominator is 0).
///
/// Participation is intentionally two separate numbers, never one invented
/// metric:
///  - EngagementRatePercentage: % of registered Users who have ever created
///    at least one VehicleAssignment (active or historical), i.e. the union
///    of GetAllActiveAsync() and GetHistoryAsync(null, null) by UserId.
///  - ComplianceRatePercentage: the existing, approved
///    LotteryEligibilityService.CalculateCompletionPercentage, computed per
///    User exactly as GetUserDashboardUseCase computes it for a single user
///    (GetCompletedCountAsync over that user's active assignment's
///    StartDate..EndDate), then averaged across Users who currently have an
///    active assignment. This stays on Flight.Date -- it is NOT switched to
///    Flight.CreatedAt.
/// </summary>
public class GetManagerDashboardUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly LotteryEligibilityService _lotteryEligibilityService;

    public GetManagerDashboardUseCase(
        IUserRepository userRepository,
        IFlightRepository flightRepository,
        IVehicleRepository vehicleRepository,
        IVehicleAssignmentRepository vehicleAssignmentRepository,
        LotteryEligibilityService lotteryEligibilityService)
    {
        _userRepository = userRepository;
        _flightRepository = flightRepository;
        _vehicleRepository = vehicleRepository;
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _lotteryEligibilityService = lotteryEligibilityService;
    }

    public async Task<ManagerDashboardResponse> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var allFlights = await _flightRepository.GetAllAsync(null, null, null, null, cancellationToken);
        var vehicles = await _vehicleRepository.GetAllAsync(cancellationToken);
        var activeAssignments = await _vehicleAssignmentRepository.GetAllActiveAsync(cancellationToken);
        var historicalAssignments = await _vehicleAssignmentRepository.GetHistoryAsync(null, null, cancellationToken);

        var vehiclesByStatus = new VehicleStatusBreakdown
        {
            Available = vehicles.Count(v => v.Status == VehicleStatus.Available),
            InUse = vehicles.Count(v => v.Status == VehicleStatus.InUse),
            Maintenance = vehicles.Count(v => v.Status == VehicleStatus.Maintenance),
            Unavailable = vehicles.Count(v => v.Status == VehicleStatus.Unavailable),
            Retired = vehicles.Count(v => v.Status == VehicleStatus.Retired)
        };

        var utilizationDenominator = vehicles.Count - vehiclesByStatus.Retired;
        var utilization = utilizationDenominator > 0
            ? (double)vehiclesByStatus.InUse * 100 / utilizationDenominator
            : 0;

        var usersWithAnyAssignment = activeAssignments.Select(a => a.UserId)
            .Concat(historicalAssignments.Select(a => a.UserId))
            .Distinct()
            .Count();
        var engagementRate = users.Count > 0
            ? (double)usersWithAnyAssignment * 100 / users.Count
            : 0;

        double complianceRate = 0;
        if (activeAssignments.Count > 0)
        {
            var complianceSum = 0.0;
            var countedUsers = 0;

            foreach (var assignment in activeAssignments)
            {
                var user = users.FirstOrDefault(u => u.Id == assignment.UserId);
                if (user is null)
                    continue;

                var completedFlights = await _flightRepository.GetCompletedCountAsync(
                    assignment.UserId, assignment.StartDate, assignment.EndDate, cancellationToken);

                complianceSum += _lotteryEligibilityService.CalculateCompletionPercentage(user, completedFlights);
                countedUsers++;
            }

            complianceRate = countedUsers > 0 ? complianceSum / countedUsers : 0;
        }

        return new ManagerDashboardResponse
        {
            TotalUsers = users.Count,
            TotalFlights = allFlights.Count,
            ActiveAssignments = activeAssignments.Count,
            VehiclesByStatus = vehiclesByStatus,
            CurrentUtilizationPercentage = utilization,
            EngagementRatePercentage = engagementRate,
            ComplianceRatePercentage = complianceRate
        };
    }
}
