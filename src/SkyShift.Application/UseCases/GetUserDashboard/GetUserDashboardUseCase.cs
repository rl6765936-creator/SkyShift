using AutoMapper;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Services;

namespace SkyShift.Application.UseCases.GetUserDashboard;

/// <summary>
/// Aggregates the MVP dashboard data. Application performs no business
/// calculation itself: ExpectedMonthlyTrips comes from User, Percentage and
/// IsLotteryEligible both come from LotteryEligibilityService. Application's
/// only job here is fetching the right data and assembling the response.
/// CompletedFlights is counted strictly within the active VehicleAssignment's
/// StartDate..EndDate — if there is no active assignment, CompletedFlights
/// and CurrentVehicle are 0/null, not an error.
/// </summary>
public class GetUserDashboardUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly LotteryEligibilityService _lotteryEligibilityService;
    private readonly IMapper _mapper;

    public GetUserDashboardUseCase(
        IUserRepository userRepository,
        IVehicleAssignmentRepository vehicleAssignmentRepository,
        IVehicleRepository vehicleRepository,
        IFlightRepository flightRepository,
        LotteryEligibilityService lotteryEligibilityService,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
        _vehicleRepository = vehicleRepository;
        _flightRepository = flightRepository;
        _lotteryEligibilityService = lotteryEligibilityService;
        _mapper = mapper;
    }

    public async Task<DashboardResponse> ExecuteAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
            throw new KeyNotFoundException($"User {userId} was not found.");

        var activeAssignment = await _vehicleAssignmentRepository.GetActiveForUserAsync(userId, cancellationToken);

        var completedFlights = 0;
        VehicleResponse? currentVehicle = null;

        if (activeAssignment is not null)
        {
            completedFlights = await _flightRepository.GetCompletedCountAsync(
                userId, activeAssignment.StartDate, activeAssignment.EndDate, cancellationToken);

            var vehicle = await _vehicleRepository.GetByIdAsync(activeAssignment.VehicleId, cancellationToken);
            if (vehicle is not null)
            {
                currentVehicle = _mapper.Map<VehicleResponse>(vehicle);
            }
        }

        var expectedMonthlyTrips = user.CalculateExpectedMonthlyTrips();
        var percentage = _lotteryEligibilityService.CalculateCompletionPercentage(user, completedFlights);
        var isEligible = _lotteryEligibilityService.IsEligible(user, completedFlights);

        return new DashboardResponse
        {
            ExpectedMonthlyTrips = expectedMonthlyTrips,
            CompletedFlights = completedFlights,
            Percentage = percentage,
            IsLotteryEligible = isEligible,
            CurrentVehicle = currentVehicle
        };
    }
}
