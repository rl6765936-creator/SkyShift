using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkyShift.Application.Abstractions;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using SkyShift.Infrastructure.Security;

namespace SkyShift.Infrastructure;

/// <summary>
/// Registers the Infrastructure implementations (DbContext, repositories, UnitOfWork, password
/// hasher). It takes the connection string as a plain value, so reading configuration stays in the
/// Api's composition root.
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
        services.AddScoped<IVehicleCatalogRepository, VehicleCatalogRepository>();
        services.AddScoped<IVehicleAssignmentRepository, VehicleAssignmentRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
