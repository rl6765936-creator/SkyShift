using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Services;

namespace SkyShift.Application.UseCases.GetManagerDashboard;

/// <summary>
/// Builds the Manager dashboard: totals, vehicle status breakdown, utilization (InUse / (Total -
/// Retired), 0 when that is 0), engagement rate and the compliance rate of users with an active
/// assignment. Flight counts come from the database in one batched query for all active assignments
/// plus one total count, never a query per assignment.
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
        var totalFlights = await _flightRepository.CountAsync(cancellationToken);
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
            var usersById = users.ToDictionary(u => u.Id);

            // One query for all active assignments (each with its own date window),
            // instead of one count query per assignment.
            var completedByAssignment = await _flightRepository.GetCompletedCountsForAssignmentsAsync(
                activeAssignments.Select(a => a.Id).ToList(), cancellationToken);

            foreach (var assignment in activeAssignments)
            {
                if (!usersById.TryGetValue(assignment.UserId, out var user))
                    continue;

                completedByAssignment.TryGetValue(assignment.Id, out var completedFlights);

                complianceSum += _lotteryEligibilityService.CalculateCompletionPercentage(user, completedFlights);
                countedUsers++;
            }

            complianceRate = countedUsers > 0 ? complianceSum / countedUsers : 0;
        }

        return new ManagerDashboardResponse
        {
            TotalUsers = users.Count,
            TotalFlights = totalFlights,
            ActiveAssignments = activeAssignments.Count,
            VehiclesByStatus = vehiclesByStatus,
            CurrentUtilizationPercentage = utilization,
            EngagementRatePercentage = engagementRate,
            ComplianceRatePercentage = complianceRate
        };
    }
}
