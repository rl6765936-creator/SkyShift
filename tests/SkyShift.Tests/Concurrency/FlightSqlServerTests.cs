using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SkyShift.Domain.Entities;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.RecordFlight;
using SkyShift.Domain.Exceptions;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using Xunit;

namespace SkyShift.Tests.Concurrency;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_SQLSERVER")))
            Skip = "Set SKYSHIFT_TEST_SQLSERVER to a test SQL Server with permission to create temporary databases.";
    }
}

/// <summary>Always creates a uniquely named test database; never migrates the configured user database.</summary>
public class FlightSqlServerTests
{
    private static DbContextOptions<SkyShiftDbContext> Options()
    {
        var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_SQLSERVER"))
        {
            InitialCatalog = "SkyShiftTests_" + Guid.NewGuid().ToString("N")
        };
        return new DbContextOptionsBuilder<SkyShiftDbContext>().UseSqlServer(connection.ConnectionString).Options;
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task ConcurrentDuplicateReports_OnlyOneCommits_AndOtherUserCanReportSameTime()
    {
        var options = Options();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var user = User.Create("First", "first@example.test", 1, "hash");
            var other = User.Create("Other", "other@example.test", 1, "hash");
            var vehicle = Vehicle.Create(false, DateTime.UtcNow);
            database.Vehicles.Add(vehicle);
            database.Users.AddRange(user, other);
            await database.SaveChangesAsync();
            var date = DateTime.UtcNow.AddHours(-1);
            database.VehicleAssignments.Add(VehicleAssignment.Create(user.Id, vehicle.Id, date.AddDays(-1)));
            await database.SaveChangesAsync();
            var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            async Task<bool> Save()
            {
                await using var context = new SkyShiftDbContext(options);
                await gate.Task;
                try
                {
                    var useCase = new RecordFlightUseCase(new VehicleAssignmentRepository(context),
                        new FlightRepository(context), new UnitOfWork(context), TimeProvider.System);
                    await useCase.ExecuteAsync(user.Id, new RecordFlightRequest { Date = date });
                    return true;
                }
                catch (DuplicateFlightException) { return false; }
            }

            var first = Save();
            var second = Save();
            gate.SetResult(true);
            var results = await Task.WhenAll(first, second);
            Assert.Single(results, success => success);
            Assert.Equal(1, await database.Flights.CountAsync());

            database.Flights.Add(Flight.Create(other.Id, null, date, DateTime.UtcNow));
            await database.SaveChangesAsync();
            Assert.Equal(2, await database.Flights.CountAsync());
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task Migration_WithExistingDuplicates_StopsBeforeChanges_AndPreservesReports()
    {
        var options = Options();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            var migrator = database.GetService<IMigrator>();
            await migrator.MigrateAsync("20260910002608_AddManagerRoleAndTimestamps");
            await database.Database.ExecuteSqlRawAsync(@"
SET IDENTITY_INSERT [Users] ON;
INSERT INTO [Users] ([Id], [Name], [Email], [AverageDailyTrips], [PasswordHash])
VALUES (1, N'Test', N'test@example.test', 1, N'hash');
SET IDENTITY_INSERT [Users] OFF;
INSERT INTO [Flights] ([UserId], [Date]) VALUES (1, '2026-10-06T12:00:00'), (1, '2026-10-06T12:00:00');");
            var error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync());
            Assert.Equal(51000, error.Number);
            Assert.Equal(2, await database.Flights.CountAsync());
            await database.Database.OpenConnectionAsync();
            await using var command = database.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT COL_LENGTH('Users', 'Gender')";
            Assert.Equal(DBNull.Value, await command.ExecuteScalarAsync());
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }
}
