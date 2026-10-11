using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkyShift.Application.Abstractions;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using SkyShift.Infrastructure.Security;

namespace SkyShift.Infrastructure;

/// <summary>
/// Registers the Infrastructure implementations (DbContext, repositories, UnitOfWork, password hasher)
/// from a plain connection string and provider, so configuration stays in the Api's composition root.
/// PostgreSQL uses its own migrations assembly, leaving the SQL Server migrations untouched.
/// </summary>
public static class DependencyInjection
{
    public const string PostgreSqlMigrationsAssembly = "SkyShift.Infrastructure.PostgreSql";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString,
        DatabaseProvider provider = DatabaseProvider.SqlServer)
    {
        services.AddDbContext<SkyShiftDbContext>(options => UseProvider(options, connectionString, provider));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IVehicleCatalogRepository, VehicleCatalogRepository>();
        services.AddScoped<IVehicleAssignmentRepository, VehicleAssignmentRepository>();
        services.AddScoped<IFlightRepository, FlightRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }

    /// <summary>
    /// Configures the selected provider. No retrying execution strategy is enabled for either
    /// provider: the critical paths run in explicit transactions that cannot be replayed safely.
    /// </summary>
    public static DbContextOptionsBuilder UseProvider(DbContextOptionsBuilder options, string connectionString, DatabaseProvider provider) =>
        provider switch
        {
            DatabaseProvider.SqlServer => options.UseSqlServer(connectionString),
            DatabaseProvider.PostgreSql => options.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsAssembly(PostgreSqlMigrationsAssembly)),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
}
