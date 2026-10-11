using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.GetUserDashboard;
using SkyShift.Application.UseCases.GetUserVehicle;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Services;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using Xunit;

namespace SkyShift.Tests.Persistence;

/// <summary>
/// Dashboard and "my vehicle" are display reads: untracked and outside the
/// SelectVehicle Serializable transaction. The write path keeps both.
/// </summary>
public class ReadOnlyVehicleQueryTests
{
    // SkyShiftDbContext's ambient transaction (EnsureTransactionAsync). The InMemory
    // provider does not report Database.CurrentTransaction, so the field is checked directly.
    private static bool HasAmbientTransaction(SkyShiftDbContext context) =>
        typeof(SkyShiftDbContext).GetField("_currentTransaction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(context) is not null;

    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static DbContextOptions<SkyShiftDbContext> Options() => new DbContextOptionsBuilder<SkyShiftDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

    private static async Task<(int UserId, int VehicleId)> SeedAssignedUserAsync(DbContextOptions<SkyShiftDbContext> options)
    {
        await using var seed = new SkyShiftDbContext(options);
        var user = User.Create("Reader", "reader@example.test", 2, "hash");
        var vehicle = Vehicle.Create(false, DateTime.UtcNow);
        vehicle.Assign();
        seed.AddRange(user, vehicle);
        await seed.SaveChangesAsync();
        seed.VehicleAssignments.Add(VehicleAssignment.Create(user.Id, vehicle.Id, DateTime.UtcNow.AddDays(-1)));
        await seed.SaveChangesAsync();
        return (user.Id, vehicle.Id);
    }

    [Fact]
    public async Task UserDashboard_ReadsVehicleUntracked_WithoutOpeningATransaction()
    {
        var options = Options();
        var (userId, vehicleId) = await SeedAssignedUserAsync(options);
        await using var context = new SkyShiftDbContext(options);

        var dashboard = await new GetUserDashboardUseCase(new UserRepository(context), new VehicleAssignmentRepository(context),
            new VehicleRepository(context), new FlightRepository(context), new LotteryEligibilityService(), Mapper()).ExecuteAsync(userId);

        Assert.Equal(vehicleId, dashboard.CurrentVehicle!.Id);
        Assert.False(HasAmbientTransaction(context));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task MyVehicle_ReadsVehicleUntracked_WithoutOpeningATransaction()
    {
        var options = Options();
        var (userId, vehicleId) = await SeedAssignedUserAsync(options);
        await using var context = new SkyShiftDbContext(options);

        var vehicle = await new GetUserVehicleUseCase(new VehicleAssignmentRepository(context), new VehicleRepository(context), Mapper())
            .ExecuteAsync(userId);

        Assert.Equal(vehicleId, vehicle!.Id);
        Assert.False(HasAmbientTransaction(context));
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task WritePath_GetByIdAsync_StillTracks_AndJoinsTheTransaction()
    {
        var options = Options();
        var (_, vehicleId) = await SeedAssignedUserAsync(options);
        await using var context = new SkyShiftDbContext(options);

        var vehicle = await new VehicleRepository(context).GetByIdAsync(vehicleId);

        Assert.NotNull(vehicle);
        Assert.True(HasAmbientTransaction(context));
        Assert.Equal(EntityState.Unchanged, context.Entry(vehicle!).State);
    }

    [Fact]
    public async Task DisplayUseCases_NeverCallTheTransactionalLookups()
    {
        var user = User.Create("Mock", "mock@example.test", 2, "hash");
        var vehicle = Vehicle.Create(false, DateTime.UtcNow);
        var assignment = VehicleAssignment.Create(1, vehicle.Id, DateTime.UtcNow.AddDays(-1));

        // Strict: GetByIdAsync / GetActiveForUserAsync (transactional) are not set up, so any call fails.
        var vehicles = new Mock<IVehicleRepository>(MockBehavior.Strict);
        vehicles.Setup(r => r.GetByIdForReadAsync(vehicle.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        var assignments = new Mock<IVehicleAssignmentRepository>(MockBehavior.Strict);
        assignments.Setup(r => r.GetActiveForUserForReadAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(assignment);
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var flights = new Mock<IFlightRepository>();

        await new GetUserDashboardUseCase(users.Object, assignments.Object, vehicles.Object, flights.Object, new LotteryEligibilityService(), Mapper()).ExecuteAsync(1);
        await new GetUserVehicleUseCase(assignments.Object, vehicles.Object, Mapper()).ExecuteAsync(1);

        vehicles.Verify(r => r.GetByIdForReadAsync(vehicle.Id, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
