using AutoMapper;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;

namespace SkyShift.Application.Mapping;

/// <summary>
/// The project's single AutoMapper profile for entity-to-response mappings. Most members map by
/// name; road speed is mapped explicitly from catalog data, and AssignmentResponse.IsActive is set
/// by the use cases. Aggregated responses (dashboards, flight statistics, user activity) are built
/// directly in their use cases instead.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // MaxRoadSpeedKmh is exposed only when the value came from the
        // catalog (verified road speed). Legacy free-form MaxSpeedKmh values
        // keep flowing through MaxSpeedKmh unchanged.
        CreateMap<Vehicle, VehicleResponse>()
            .ForMember(dest => dest.MaxRoadSpeedKmh, opt => opt.MapFrom(src => src.CatalogItemId.HasValue ? src.MaxSpeedKmh : null));
        CreateMap<Vehicle, ManagerVehicleResponse>()
            .ForMember(dest => dest.MaxRoadSpeedKmh, opt => opt.MapFrom(src => src.CatalogItemId.HasValue ? src.MaxSpeedKmh : null));

        CreateMap<VehicleCatalogItem, VehicleCatalogResponse>()
            .ForMember(dest => dest.MaxSpeedKmh, opt => opt.MapFrom(src => src.MaxRoadSpeedKmh));

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
