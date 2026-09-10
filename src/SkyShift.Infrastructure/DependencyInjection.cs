using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkyShift.Application.Abstractions;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using SkyShift.Infrastructure.Security;

namespace SkyShift.Infrastructure;

/// <summary>
/// Composition-root extension for registering Infrastructure's
/// implementations. Takes the connection string as a plain value rather than
/// IConfiguration, so Infrastructure does not need a 4th package just to read
/// configuration -- resolving the actual connection string (appsettings,
/// environment, secrets) is left entirely to the caller (SkyShift.Api, once
/// it exists), consistent with "connection string handling deferred to the
/// future API" in the approved plan.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SkyShiftDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleAssignmentRepository, VehicleAssignmentRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
