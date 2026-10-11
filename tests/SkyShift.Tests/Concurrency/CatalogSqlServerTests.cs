using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using Xunit;

namespace SkyShift.Tests.Concurrency;

/// <summary>
/// Real SQL Server checks for the catalog. Each test creates and drops its
/// own uniquely named database (SkyShiftTests_*) and never touches the
/// application database. Skipped unless SKYSHIFT_TEST_SQLSERVER is set.
/// </summary>
public class CatalogSqlServerTests
{
    private static DbContextOptions<SkyShiftDbContext> Options()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_SQLSERVER"))
        {
            InitialCatalog = "SkyShiftTests_" + Guid.NewGuid().ToString("N")
        };
        return new DbContextOptionsBuilder<SkyShiftDbContext>().UseSqlServer(connection.ConnectionString).Options;
    }

    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task ConcurrentCreates_ForSameCatalogItem_CreateExactlyOneVehicle()
    {
        var options = Options();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<bool> Create()
            {
                await using var context = new SkyShiftDbContext(options);
                var useCase = new CreateVehicleUseCase(new VehicleRepository(context), new VehicleCatalogRepository(context),
                    new UnitOfWork(context), Mapper());
                await gate.Task;
                try
                {
                    await useCase.ExecuteAsync(new CreateVehicleRequest { CatalogItemId = 5 });
                    return true;
                }
                catch (CatalogItemInUseException)
                {
                    return false;
                }
            }

            var attempts = Enumerable.Range(0, 4).Select(_ => Create()).ToList();
            gate.SetResult(true);
            var results = await Task.WhenAll(attempts);

            Assert.Single(results, success => success);
            Assert.Equal(1, await database.Vehicles.CountAsync(v => v.CatalogItemId == 5));
            Assert.DoesNotContain(await new VehicleCatalogRepository(database).GetUnusedAsync(), item => item.Id == 5);
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task CatalogMigration_PreservesExistingVehiclesAssignmentsAndFlights()
    {
        var options = Options();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            var migrator = database.GetService<IMigrator>();
            await migrator.MigrateAsync("20261006230426_AddFlightGuardsGenderAndVehicleDetails");
            await database.Database.ExecuteSqlRawAsync(@"
SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [Name], [Email], [AverageDailyTrips], [PasswordHash], [Role], [Gender])
VALUES (7, N'Legacy', N'legacy@example.test', 2, N'hash', N'User', NULL);
SET IDENTITY_INSERT [Users] OFF;
SET IDENTITY_INSERT [Vehicles] ON;
INSERT INTO [Vehicles] ([Id], [Status], [IsPersonalExperimental], [CreatedAt], [ModelName], [MaxSpeedKmh], [Seats], [ImageUrl])
VALUES (41, N'InUse', 1, '2026-09-01T08:00:00', N'Legacy model', 180, 4, N'/catalog/car-04.webp'),
       (42, N'Retired', 0, '2026-09-02T08:00:00', NULL, NULL, NULL, NULL);
SET IDENTITY_INSERT [Vehicles] OFF;
INSERT INTO [VehicleAssignments] ([UserId], [VehicleId], [StartDate], [EndDate])
VALUES (7, 41, '2026-10-01T09:00:00', '2026-11-01T09:00:00');
INSERT INTO [Flights] ([UserId], [VehicleId], [Date], [CreatedAt])
VALUES (7, 41, '2026-10-02T10:00:00', '2026-10-02T10:05:00');");

            await migrator.MigrateAsync();

            var legacy = await database.Vehicles.AsNoTracking().SingleAsync(v => v.Id == 41);
            Assert.Null(legacy.CatalogItemId);
            Assert.Equal("Legacy model", legacy.ModelName);
            Assert.Equal(180, legacy.MaxSpeedKmh);
            Assert.Equal(4, legacy.Seats);
            Assert.Equal("/catalog/car-04.webp", legacy.ImageUrl);
            Assert.Equal(new DateTime(2026, 9, 1, 8, 0, 0), legacy.CreatedAt);
            Assert.Equal(VehicleStatus.InUse, legacy.Status);
            Assert.Null((await database.Vehicles.AsNoTracking().SingleAsync(v => v.Id == 42)).CatalogItemId);
            Assert.Equal(1, await database.VehicleAssignments.CountAsync(a => a.VehicleId == 41 && a.UserId == 7));
            Assert.Equal(1, await database.Flights.CountAsync(f => f.VehicleId == 41));
            Assert.Equal(22, await database.VehicleCatalogItems.CountAsync());

            // Legacy vehicles are unlinked, so every catalog item is still available.
            Assert.Equal(22, (await new VehicleCatalogRepository(database).GetUnusedAsync()).Count);
            Assert.Null(legacy.SeatsAccuracy);
            Assert.Null(legacy.MaxRoadSpeedAccuracy);
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task SpecificationMigration_CompletesCatalog_AndKeepsAlreadyCreatedCatalogVehicleUnchanged()
    {
        var options = Options();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            var migrator = database.GetService<IMigrator>();
            await migrator.MigrateAsync("20261010203000_AddVehicleCatalog");
            // A vehicle created from catalog item 3 under the previous rules (no specs at that time).
            await database.Database.ExecuteSqlRawAsync(@"
SET IDENTITY_INSERT [Vehicles] ON;
INSERT INTO [Vehicles] ([Id], [Status], [IsPersonalExperimental], [CreatedAt], [CatalogItemId], [Manufacturer], [ModelName],
    [MaxSpeedKmh], [Seats], [ImageUrl], [Description], [DescriptionHe], [CatalogIdentification])
VALUES (51, N'Maintenance', 0, '2026-10-10T08:00:00', 3, N'Koenigsegg', N'Regera', NULL, NULL, N'/catalog/car-03.webp',
    N'Old description', N'תיאור ישן', N'Identified');
SET IDENTITY_INSERT [Vehicles] OFF;");

            await migrator.MigrateAsync();

            var existing = await database.Vehicles.AsNoTracking().SingleAsync(v => v.Id == 51);
            Assert.Equal(3, existing.CatalogItemId);
            Assert.Null(existing.Seats);
            Assert.Null(existing.MaxSpeedKmh);
            Assert.Null(existing.SeatsAccuracy);
            Assert.Null(existing.MaxRoadSpeedAccuracy);
            Assert.Equal("Old description", existing.Description);
            Assert.Equal(VehicleStatus.Maintenance, existing.Status);
            Assert.Equal(new DateTime(2026, 10, 10, 8, 0, 0), existing.CreatedAt);

            var catalog = await database.VehicleCatalogItems.AsNoTracking().ToListAsync();
            Assert.Equal(22, catalog.Count);
            Assert.All(catalog, item =>
            {
                Assert.InRange(item.Seats, 1, 100);
                Assert.InRange(item.MaxRoadSpeedKmh, 1, 600);
                Assert.False(string.IsNullOrWhiteSpace(item.DescriptionHe));
            });
            Assert.DoesNotContain(await new VehicleCatalogRepository(database).GetUnusedAsync(), item => item.Id == 3);
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }
}
