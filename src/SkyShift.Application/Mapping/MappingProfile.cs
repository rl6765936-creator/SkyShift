using AutoMapper;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.Mapping;

/// <summary>
/// The single AutoMapper profile for this project. Registers only the
/// mappings that replace what were previously plain, manual, field-by-field
/// "new XResponse { ... }" object-initializer copies in the Use Cases --
/// every property on every source/destination pair here already matched by
/// name (Domain entity getters are public even though their setters are
/// private, so AutoMapper's default convention resolves them exactly as
/// the manual copies did), so no custom conversion logic was ever involved,
/// and none is introduced here.
///
/// Deliberately NOT used for GetFlightStatsUseCase,
/// GetManagerDashboardUseCase, GetAllUsersActivityUseCase, or the
/// DashboardResponse/ManagerDashboardResponse construction in
/// GetUserDashboardUseCase/GetManagerDashboardUseCase -- those all build
/// their response from grouped/aggregated/multi-source data (counts,
/// percentages, values combined from more than one repository call or
/// Domain service), not from a single entity's own properties, so there is
/// no manual Entity <-> DTO mapping there for AutoMapper to replace.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Vehicle, VehicleResponse>();
        CreateMap<Vehicle, ManagerVehicleResponse>();
        CreateMap<Flight, FlightResponse>();
        CreateMap<User, LoginResponse>();
        CreateMap<User, RegisterUserResponse>();

        // IsActive is computed via the Domain method
        // VehicleAssignment.IsActive(DateTime.UtcNow), not a stored
        // property, so it cannot be matched by AutoMapper's default
        // by-name convention. Left unmapped here and assigned explicitly
        // by the Use Case after mapping the rest, exactly as the manual
        // mapping did.
        CreateMap<VehicleAssignment, AssignmentResponse>()
            .ForMember(dest => dest.IsActive, opt => opt.Ignore());
    }
}
