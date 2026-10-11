using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;

namespace SkyShift.Application.UseCases.GetAllUsersActivity;

/// <summary>
/// Manager user activity: raw per-user facts (TotalFlights, HasActiveAssignment,
/// LastFlightCreatedAt), with no invented "active user" flag. It aggregates in memory over
/// IFlightRepository.GetAllAsync and IVehicleAssignmentRepository.GetAllActiveAsync.
/// </summary>
public class GetAllUsersActivityUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IFlightRepository _flightRepository;
    private readonly IVehicleAssignmentRepository _vehicleAssignmentRepository;

    public GetAllUsersActivityUseCase(
        IUserRepository userRepository,
        IFlightRepository flightRepository,
        IVehicleAssignmentRepository vehicleAssignmentRepository)
    {
        _userRepository = userRepository;
        _flightRepository = flightRepository;
        _vehicleAssignmentRepository = vehicleAssignmentRepository;
    }

    public async Task<IReadOnlyList<UserActivityResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var allFlights = await _flightRepository.GetAllAsync(null, null, null, null, cancellationToken);
        var activeAssignments = await _vehicleAssignmentRepository.GetAllActiveAsync(cancellationToken);

        var usersWithActiveAssignment = activeAssignments
            .Select(a => a.UserId)
            .ToHashSet();

        var flightsByUser = allFlights
            .GroupBy(f => f.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return users
            .Select(u =>
            {
                flightsByUser.TryGetValue(u.Id, out var userFlights);
                userFlights ??= new List<Domain.Entities.Flight>();

                return new UserActivityResponse
                {
                    UserId = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    TotalFlights = userFlights.Count,
                    HasActiveAssignment = usersWithActiveAssignment.Contains(u.Id),
                    LastFlightCreatedAt = userFlights.Count > 0
                        ? userFlights.Max(f => f.CreatedAt)
                        : null
                };
            })
            .ToList();
    }
}
