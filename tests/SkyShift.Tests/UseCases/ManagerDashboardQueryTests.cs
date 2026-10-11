using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.GetManagerDashboard;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Services;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using SkyShift.Tests.Concurrency;
using Xunit;

namespace SkyShift.Tests.UseCases;

/// <summary>
/// The manager dashboard must not issue one flight-count query per active
/// assignment (N+1), must not load all flights to count them, and must keep the
/// exact per-assignment date window rule of GetCompletedCountAsync.
/// </summary>
public class ManagerDashboardQueryTests
{
    private static GetManagerDashboardUseCase UseCase(SkyShiftDbContext context) => new(
        new UserRepository(context), new FlightRepository(context), new VehicleRepository(context),
        new VehicleAssignmentRepository(context), new LotteryEligibilityService());

    // Seeds users, active and expired assignments and flights on and around the window edges.
    private static async Task<List<VehicleAssignment>> SeedAsync(SkyShiftDbContext context, int activeAssignments)
    {
        var now = DateTime.UtcNow;
        var assignments = new List<VehicleAssignment>();
        for (var i = 0; i < activeAssignments; i++)
        {
            var user = User.Create($"User {i}", $"u{i}-{Guid.NewGuid():N}@example.test", 1 + i % 3, "hash");
            var vehicle = Vehicle.Create(false, now);
            context.AddRange(user, vehicle);
            await context.SaveChangesAsync();
            var start = now.AddDays(-5 - i);
            var assignment = VehicleAssignment.Create(user.Id, vehicle.Id, start);
            context.VehicleAssignments.Add(assignment);
            context.Flights.AddRange(
                Flight.Create(user.Id, vehicle.Id, start, now),                  // exactly on StartDate: counted
                Flight.Create(user.Id, vehicle.Id, start.AddHours(1), now),      // inside: counted
                Flight.Create(user.Id, vehicle.Id, assignment.EndDate, now),     // exactly on EndDate: counted
                Flight.Create(user.Id, vehicle.Id, start.AddTicks(-1), now),     // just before: not counted
                Flight.Create(user.Id, vehicle.Id, assignment.EndDate.AddTicks(1), now)); // just after: not counted
            for (var extra = 0; extra < i; extra++)
                context.Flights.Add(Flight.Create(user.Id, vehicle.Id, start.AddHours(2 + extra), now));
            assignments.Add(assignment);
        }

        // A user whose assignment expired, and a user without any assignment.
        var past = User.Create("Past", $"past-{Guid.NewGuid():N}@example.test", 2, "hash");
        var idle = User.Create("Idle", $"idle-{Guid.NewGuid():N}@example.test", 2, "hash");
        var oldVehicle = Vehicle.Create(false, now);
        context.AddRange(past, idle, oldVehicle);
        await context.SaveChangesAsync();
        context.VehicleAssignments.Add(VehicleAssignment.Create(past.Id, oldVehicle.Id, now.AddMonths(-3)));
        context.Flights.Add(Flight.Create(past.Id, oldVehicle.Id, now.AddMonths(-3).AddDays(1), now));
        await context.SaveChangesAsync();
        return assignments;
    }

    // The previous algorithm: one GetCompletedCountAsync call per active assignment.
    private static async Task<double> ReferenceComplianceAsync(SkyShiftDbContext context)
    {
        var flights = new FlightRepository(context);
        var service = new LotteryEligibilityService();
        var users = await context.Users.AsNoTracking().ToListAsync();
        var active = await new VehicleAssignmentRepository(context).GetAllActiveAsync();
        var sum = 0.0;
        var counted = 0;
        foreach (var assignment in active)
        {
            var user = users.First(u => u.Id == assignment.UserId);
            sum += service.CalculateCompletionPercentage(user,
                await flights.GetCompletedCountAsync(assignment.UserId, assignment.StartDate, assignment.EndDate));
            counted++;
        }
        return counted > 0 ? sum / counted : 0;
    }

    [Fact]
    public async Task Dashboard_MatchesPreviousPerAssignmentResults_IncludingWindowBoundaries()
    {
        var options = new DbContextOptionsBuilder<SkyShiftDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new SkyShiftDbContext(options);
        var assignments = await SeedAsync(context, activeAssignments: 4);

        var counts = await new FlightRepository(context).GetCompletedCountsForAssignmentsAsync(assignments.Select(a => a.Id).ToList());
        for (var i = 0; i < assignments.Count; i++)
            Assert.Equal(3 + i, counts[assignments[i].Id]); // start, inside, end (+ i extras); edges outside excluded

        var dashboard = await UseCase(context).ExecuteAsync();

        Assert.Equal(await ReferenceComplianceAsync(context), dashboard.ComplianceRatePercentage, precision: 10);
        Assert.Equal(await context.Flights.CountAsync(), dashboard.TotalFlights);
        Assert.Equal(4, dashboard.ActiveAssignments);
        Assert.Equal(6, dashboard.TotalUsers);
    }

    [Fact]
    public async Task BatchedCount_WithNoAssignments_ReturnsEmptyWithoutQuerying()
    {
        var options = new DbContextOptionsBuilder<SkyShiftDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new SkyShiftDbContext(options);
        Assert.Empty(await new FlightRepository(context).GetCompletedCountsForAssignmentsAsync(Array.Empty<int>()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    public async Task Dashboard_RepositoryCalls_DoNotGrowWithAssignments(int activeCount)
    {
        var users = Enumerable.Range(1, activeCount).Select(i =>
        {
            var user = User.Create($"U{i}", $"u{i}@example.test", 2, "hash");
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, i); // as if loaded from the database
            return user;
        }).ToList();
        var now = DateTime.UtcNow;
        var active = users.Select((u, i) => VehicleAssignment.Create(i + 1, i + 1, now.AddDays(-1))).ToList();

        var userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);
        var vehicleRepository = new Mock<IVehicleRepository>(MockBehavior.Strict);
        vehicleRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Vehicle>());
        var assignmentRepository = new Mock<IVehicleAssignmentRepository>(MockBehavior.Strict);
        assignmentRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(active);
        assignmentRepository.Setup(r => r.GetHistoryAsync(null, null, It.IsAny<CancellationToken>())).ReturnsAsync(new List<VehicleAssignment>());
        // Strict: GetCompletedCountAsync (per assignment) and GetAllAsync (all flights) are not set up, so any call fails.
        var flightRepository = new Mock<IFlightRepository>(MockBehavior.Strict);
        flightRepository.Setup(r => r.CountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);
        flightRepository.Setup(r => r.GetCompletedCountsForAssignmentsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await new GetManagerDashboardUseCase(userRepository.Object, flightRepository.Object,
            vehicleRepository.Object, assignmentRepository.Object, new LotteryEligibilityService()).ExecuteAsync();

        Assert.Equal(42, result.TotalFlights);
        flightRepository.Verify(r => r.GetCompletedCountsForAssignmentsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        flightRepository.Verify(r => r.CountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class CommandCounter : DbCommandInterceptor
    {
        public int Count;
        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        { Interlocked.Increment(ref Count); return result; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Count); return ValueTask.FromResult(result); }
        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Count); return ValueTask.FromResult(result); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task Dashboard_SqlCommandCount_IsConstant_AndResultsMatchPreviousAlgorithm()
    {
        async Task<(int Commands, ManagerDashboardResponse Result, double Reference)> Run(int activeCount)
        {
            var connection = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_SQLSERVER"))
            { InitialCatalog = "SkyShiftTests_" + Guid.NewGuid().ToString("N") }.ConnectionString;
            var plain = new DbContextOptionsBuilder<SkyShiftDbContext>().UseSqlServer(connection).Options;
            await using var setup = new SkyShiftDbContext(plain);
            try
            {
                await setup.Database.MigrateAsync();
                await SeedAsync(setup, activeCount);
                var counter = new CommandCounter();
                await using var measured = new SkyShiftDbContext(
                    new DbContextOptionsBuilder<SkyShiftDbContext>().UseSqlServer(connection).AddInterceptors(counter).Options);
                var result = await UseCase(measured).ExecuteAsync();
                return (counter.Count, result, await ReferenceComplianceAsync(setup));
            }
            finally { await setup.Database.EnsureDeletedAsync(); }
        }

        var one = await Run(1);
        var many = await Run(6);

        Assert.Equal(one.Commands, many.Commands); // no per-assignment query
        Assert.Equal(one.Reference, one.Result.ComplianceRatePercentage, precision: 10);
        Assert.Equal(many.Reference, many.Result.ComplianceRatePercentage, precision: 10);
        Assert.Equal(6, many.Result.ActiveAssignments);
    }
}
