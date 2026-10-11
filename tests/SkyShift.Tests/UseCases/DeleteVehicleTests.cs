using System.Data.Common;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.DeleteVehicle;
using SkyShift.Application.UseCases.SelectVehicle;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using SkyShift.Tests.Concurrency;
using Xunit;

namespace SkyShift.Tests.UseCases;

/// <summary>
/// DELETE /api/manager/vehicles/{id}: only a never-used Available vehicle can be
/// deleted; history is never removed; the catalog model returns to the pool.
/// </summary>
public class DeleteVehicleTests
{
    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static DbContextOptions<SkyShiftDbContext> InMemory() => new DbContextOptionsBuilder<SkyShiftDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

    private static DeleteVehicleUseCase Delete(SkyShiftDbContext context) => new(new VehicleRepository(context), new UnitOfWork(context));

    private static async Task<int> CreateFromCatalogAsync(DbContextOptions<SkyShiftDbContext> options, int catalogItemId)
    {
        await using var context = new SkyShiftDbContext(options);
        var created = await new CreateVehicleUseCase(new VehicleRepository(context), new VehicleCatalogRepository(context),
            new UnitOfWork(context), Mapper()).ExecuteAsync(new CreateVehicleRequest { CatalogItemId = catalogItemId });
        return created.Id;
    }

    [Fact]
    public async Task UnusedAvailableVehicle_IsDeleted_AndItsCatalogModelReturnsToThePool()
    {
        var options = InMemory();
        await using (var seed = new SkyShiftDbContext(options))
            await seed.Database.EnsureCreatedAsync(); // catalog seed
        var id = await CreateFromCatalogAsync(options, 3);

        await using (var context = new SkyShiftDbContext(options))
            await Delete(context).ExecuteAsync(id);

        await using var check = new SkyShiftDbContext(options);
        Assert.False(await check.Vehicles.AnyAsync(v => v.Id == id));
        Assert.Contains(await new VehicleCatalogRepository(check).GetUnusedAsync(), item => item.Id == 3);
        Assert.NotEqual(id, await CreateFromCatalogAsync(options, 3)); // model can be added to the fleet again
    }

    [Fact]
    public async Task MissingVehicle_IsNotFound()
    {
        await using var context = new SkyShiftDbContext(InMemory());
        var error = await Assert.ThrowsAsync<VehicleNotFoundException>(() => Delete(context).ExecuteAsync(404));
        Assert.Equal(404, error.VehicleId);
    }

    [Theory]
    [InlineData(VehicleStatus.InUse)]
    [InlineData(VehicleStatus.Maintenance)]
    [InlineData(VehicleStatus.Unavailable)]
    [InlineData(VehicleStatus.Retired)]
    public async Task VehicleThatIsNotAvailable_IsKept(VehicleStatus status)
    {
        var options = InMemory();
        int id;
        await using (var seed = new SkyShiftDbContext(options))
        {
            var vehicle = Vehicle.Create(false, DateTime.UtcNow);
            if (status == VehicleStatus.InUse) vehicle.Assign();
            if (status == VehicleStatus.Maintenance) vehicle.SendToMaintenance();
            if (status == VehicleStatus.Unavailable) vehicle.MarkUnavailable();
            if (status == VehicleStatus.Retired) vehicle.Retire();
            seed.Vehicles.Add(vehicle);
            await seed.SaveChangesAsync();
            id = vehicle.Id;
        }

        await using var context = new SkyShiftDbContext(options);
        var error = await Assert.ThrowsAsync<VehicleDeletionNotAllowedException>(() => Delete(context).ExecuteAsync(id));
        Assert.Equal(VehicleDeletionNotAllowedException.NotAvailableCode, error.Code);
        await using var check = new SkyShiftDbContext(options);
        Assert.True(await check.Vehicles.AnyAsync(v => v.Id == id));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task AvailableVehicleWithHistory_IsKept_AndHistoryIsUntouched(bool withAssignment, bool withFlight)
    {
        var options = InMemory();
        int id;
        await using (var seed = new SkyShiftDbContext(options))
        {
            var user = User.Create("Past", "past@example.test", 1, "hash");
            var vehicle = Vehicle.Create(false, DateTime.UtcNow);
            seed.AddRange(user, vehicle);
            await seed.SaveChangesAsync();
            if (withAssignment) seed.VehicleAssignments.Add(VehicleAssignment.Create(user.Id, vehicle.Id, DateTime.UtcNow.AddMonths(-2)));
            if (withFlight) seed.Flights.Add(Flight.Create(user.Id, vehicle.Id, DateTime.UtcNow.AddMonths(-2), DateTime.UtcNow));
            await seed.SaveChangesAsync();
            id = vehicle.Id;
        }

        await using var context = new SkyShiftDbContext(options);
        var error = await Assert.ThrowsAsync<VehicleDeletionNotAllowedException>(() => Delete(context).ExecuteAsync(id));
        Assert.Equal(VehicleDeletionNotAllowedException.HasHistoryCode, error.Code);

        await using var check = new SkyShiftDbContext(options);
        Assert.True(await check.Vehicles.AnyAsync(v => v.Id == id));
        Assert.Equal(withAssignment ? 1 : 0, await check.VehicleAssignments.CountAsync());
        Assert.Equal(withFlight ? 1 : 0, await check.Flights.CountAsync());
    }

    [Fact]
    public async Task ConcurrentChangeDetectedAtSave_IsAClearConflict_NotAnAssignmentError()
    {
        var vehicle = Vehicle.Create(false, DateTime.UtcNow);
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(v => v.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        vehicles.Setup(v => v.HasHistoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var unit = new Mock<IUnitOfWork>();
        // What UnitOfWork raises for a Vehicle RowVersion conflict or serialization failure.
        unit.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new VehicleNotAvailableException(5, VehicleStatus.InUse));

        var error = await Assert.ThrowsAsync<VehicleDeletionNotAllowedException>(() =>
            new DeleteVehicleUseCase(vehicles.Object, unit.Object).ExecuteAsync(5));
        Assert.Equal(VehicleDeletionNotAllowedException.ChangedConcurrentlyCode, error.Code);
    }

    [Fact]
    public async Task HistoryCheck_RunsBeforeAnyDelete()
    {
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(v => v.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(Vehicle.Create(false, DateTime.UtcNow));
        vehicles.Setup(v => v.HasHistoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var unit = new Mock<IUnitOfWork>();

        await Assert.ThrowsAsync<VehicleDeletionNotAllowedException>(() => new DeleteVehicleUseCase(vehicles.Object, unit.Object).ExecuteAsync(5));
        vehicles.Verify(v => v.DeleteAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------------------------------------------------------------- real SQL Server (isolated temporary databases)

    private static DbContextOptions<SkyShiftDbContext> SqlServer() => new DbContextOptionsBuilder<SkyShiftDbContext>()
        .UseSqlServer(new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_SQLSERVER"))
        { InitialCatalog = "SkyShiftTests_" + Guid.NewGuid().ToString("N") }.ConnectionString).Options;

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task SqlServer_DeletionRules_HistoryAndForeignKeysAreRespected()
    {
        var options = SqlServer();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var user = User.Create("Past", "past@example.test", 1, "hash");
            var unused = Vehicle.Create(false, DateTime.UtcNow);
            var inUse = Vehicle.Create(false, DateTime.UtcNow);
            inUse.Assign();
            var withHistory = Vehicle.Create(false, DateTime.UtcNow);
            database.AddRange(user, unused, inUse, withHistory);
            await database.SaveChangesAsync();
            database.VehicleAssignments.Add(VehicleAssignment.Create(user.Id, withHistory.Id, DateTime.UtcNow.AddMonths(-2)));
            database.Flights.Add(Flight.Create(user.Id, withHistory.Id, DateTime.UtcNow.AddMonths(-2).AddDays(1), DateTime.UtcNow));
            await database.SaveChangesAsync();

            async Task<Exception?> TryDelete(int id)
            {
                await using var context = new SkyShiftDbContext(options);
                try { await Delete(context).ExecuteAsync(id); return null; }
                catch (Exception ex) { return ex; }
            }

            Assert.Null(await TryDelete(unused.Id));
            Assert.Equal(VehicleDeletionNotAllowedException.NotAvailableCode, Assert.IsType<VehicleDeletionNotAllowedException>(await TryDelete(inUse.Id)).Code);
            Assert.Equal(VehicleDeletionNotAllowedException.HasHistoryCode, Assert.IsType<VehicleDeletionNotAllowedException>(await TryDelete(withHistory.Id)).Code);
            Assert.IsType<VehicleNotFoundException>(await TryDelete(unused.Id));

            // Defence in depth: even if the history check were bypassed, the FK keeps history
            // and the DELETE is reported as a deletion conflict, not a 500.
            await using (var context = new SkyShiftDbContext(options))
            {
                var tracked = await context.Vehicles.SingleAsync(v => v.Id == withHistory.Id);
                context.Vehicles.Remove(tracked);
                var fk = await Assert.ThrowsAsync<VehicleDeletionNotAllowedException>(() => new UnitOfWork(context).SaveChangesAsync());
                Assert.Equal(VehicleDeletionNotAllowedException.HasHistoryCode, fk.Code);
            }

            await using var check = new SkyShiftDbContext(options);
            Assert.False(await check.Vehicles.AnyAsync(v => v.Id == unused.Id));
            Assert.True(await check.Vehicles.AnyAsync(v => v.Id == inUse.Id));
            Assert.True(await check.Vehicles.AnyAsync(v => v.Id == withHistory.Id));
            Assert.Equal(1, await check.VehicleAssignments.CountAsync());
            Assert.Equal(1, await check.Flights.CountAsync());
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task SqlServer_ConcurrentDeleteAndAssignment_ExactlyOneWins_AndStateStaysConsistent()
    {
        var options = SqlServer();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            for (var round = 0; round < 10; round++)
            {
                var user = User.Create($"Racer {round}", $"racer{round}@example.test", 1, "hash");
                var vehicle = Vehicle.Create(false, DateTime.UtcNow);
                database.AddRange(user, vehicle);
                await database.SaveChangesAsync();
                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                async Task<Exception?> DeleteIt()
                {
                    await using var context = new SkyShiftDbContext(options);
                    await gate.Task;
                    try { await Delete(context).ExecuteAsync(vehicle.Id); return null; }
                    catch (Exception ex) { return ex; }
                }

                async Task<Exception?> AssignIt()
                {
                    await using var context = new SkyShiftDbContext(options);
                    await gate.Task;
                    try
                    {
                        await new SelectVehicleUseCase(new VehicleRepository(context), new VehicleAssignmentRepository(context), new UnitOfWork(context))
                            .ExecuteAsync(user.Id, new SelectVehicleRequest { VehicleId = vehicle.Id });
                        return null;
                    }
                    catch (Exception ex) { return ex; }
                }

                var deleting = DeleteIt();
                var assigning = AssignIt();
                gate.SetResult();
                var (deleteError, assignError) = (await deleting, await assigning);

                // Only expected, mapped outcomes -- never a raw SQL/EF error (which would be a 500).
                Assert.True(deleteError is null or VehicleDeletionNotAllowedException, $"Delete failed unexpectedly: {deleteError}");
                Assert.True(assignError is null or VehicleNotAvailableException or VehicleNotFoundException, $"Assign failed unexpectedly: {assignError}");
                Assert.True((deleteError is null) ^ (assignError is null), $"Round {round}: expected exactly one winner (delete: {deleteError?.GetType().Name ?? "ok"}, assign: {assignError?.GetType().Name ?? "ok"}).");

                await using var check = new SkyShiftDbContext(options);
                var stored = await check.Vehicles.AsNoTracking().SingleOrDefaultAsync(v => v.Id == vehicle.Id);
                var assignmentCount = await check.VehicleAssignments.CountAsync(a => a.VehicleId == vehicle.Id);
                if (deleteError is null)
                {
                    Assert.Null(stored);
                    Assert.Equal(0, assignmentCount);
                }
                else
                {
                    Assert.Equal(VehicleStatus.InUse, stored!.Status);
                    Assert.Equal(1, assignmentCount);
                }
            }
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    // Holds the first transaction right after its locking vehicle read (WITH UPDLOCK),
    // so the test controls which side wins while the lock is genuinely held.
    private sealed class HoldAfterVehicleLock : DbCommandInterceptor
    {
        private int _held;
        public TaskCompletionSource Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK", StringComparison.Ordinal) && Interlocked.Exchange(ref _held, 1) == 0)
            {
                Locked.SetResult();
                await Release.Task;
            }
            return result;
        }
    }

    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception ex) { return ex; }
    }

    private static Task<Exception?> DeleteWith(DbContextOptions<SkyShiftDbContext> options, int vehicleId, IInterceptor? hold = null) => Capture(async () =>
    {
        await using var context = new SkyShiftDbContext(hold is null ? options
            : new DbContextOptionsBuilder<SkyShiftDbContext>(options).AddInterceptors(hold).Options);
        await Delete(context).ExecuteAsync(vehicleId);
    });

    private static Task<Exception?> AssignWith(DbContextOptions<SkyShiftDbContext> options, int userId, int vehicleId, IInterceptor? hold = null) => Capture(async () =>
    {
        await using var context = new SkyShiftDbContext(hold is null ? options
            : new DbContextOptionsBuilder<SkyShiftDbContext>(options).AddInterceptors(hold).Options);
        await new SelectVehicleUseCase(new VehicleRepository(context), new VehicleAssignmentRepository(context), new UnitOfWork(context))
            .ExecuteAsync(userId, new SelectVehicleRequest { VehicleId = vehicleId });
    });

    private static async Task<(User User, Vehicle Vehicle)> SeedPairAsync(SkyShiftDbContext database, string name)
    {
        var user = User.Create(name, $"{name}-{Guid.NewGuid():N}@example.test", 1, "hash");
        var vehicle = Vehicle.Create(false, DateTime.UtcNow);
        database.AddRange(user, vehicle);
        await database.SaveChangesAsync();
        return (user, vehicle);
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task SqlServer_DeleteHoldsTheVehicleFirst_AssignmentWaits_ThenGets404_AndNothingIsLeftBehind()
    {
        var options = SqlServer();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var (user, vehicle) = await SeedPairAsync(database, "late");
            var hold = new HoldAfterVehicleLock();

            var deleting = DeleteWith(options, vehicle.Id, hold);
            await hold.Locked.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var assigning = AssignWith(options, user.Id, vehicle.Id);
            await Task.Delay(750);
            Assert.False(assigning.IsCompleted); // blocked on the vehicle lock, not deadlocked or racing ahead

            hold.Release.SetResult();
            Assert.Null(await deleting);
            Assert.IsType<VehicleNotFoundException>(await assigning);

            await using var check = new SkyShiftDbContext(options);
            Assert.False(await check.Vehicles.AnyAsync(v => v.Id == vehicle.Id));
            Assert.Equal(0, await check.VehicleAssignments.CountAsync(a => a.UserId == user.Id));

            // The losing transaction was rolled back and released its locks: the same user can still be assigned.
            var other = Vehicle.Create(false, DateTime.UtcNow);
            check.Vehicles.Add(other);
            await check.SaveChangesAsync();
            Assert.Null(await AssignWith(options, user.Id, other.Id).WaitAsync(TimeSpan.FromSeconds(30)));
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task SqlServer_AssignmentHoldsTheVehicleFirst_DeleteWaits_ThenGetsBilingual409_AndAssignmentIsKept()
    {
        var options = SqlServer();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var (user, vehicle) = await SeedPairAsync(database, "early");
            var hold = new HoldAfterVehicleLock();

            var assigning = AssignWith(options, user.Id, vehicle.Id, hold);
            await hold.Locked.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var deleting = DeleteWith(options, vehicle.Id);
            await Task.Delay(750);
            Assert.False(deleting.IsCompleted);

            hold.Release.SetResult();
            Assert.Null(await assigning);
            var refused = Assert.IsType<VehicleDeletionNotAllowedException>(await deleting);
            Assert.Equal(VehicleDeletionNotAllowedException.NotAvailableCode, refused.Code);

            await using var check = new SkyShiftDbContext(options);
            Assert.Equal(VehicleStatus.InUse, (await check.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id)).Status);
            Assert.Equal(1, await check.VehicleAssignments.CountAsync(a => a.VehicleId == vehicle.Id && a.UserId == user.Id));
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }

    [SqlServerFact]
    [Trait("Category", "SqlServer")]
    public async Task SqlServer_SameUserSelectingTwoVehiclesConcurrently_GetsOneAssignment_AndA409_NeverARawDeadlock()
    {
        var options = SqlServer();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            for (var round = 0; round < 5; round++)
            {
                var (user, first) = await SeedPairAsync(database, $"double{round}");
                var second = Vehicle.Create(false, DateTime.UtcNow);
                database.Vehicles.Add(second);
                await database.SaveChangesAsync();

                var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                async Task<Exception?> Select(int vehicleId) { await gate.Task; return await AssignWith(options, user.Id, vehicleId); }
                var a = Select(first.Id);
                var b = Select(second.Id);
                gate.SetResult();
                var errors = new[] { await a, await b };

                // Deadlock victims surface as VehicleNotAvailableException (409); a serialized loser as ActiveAssignmentExistsException (409).
                Assert.Single(errors, e => e is null);
                Assert.Single(errors, e => e is VehicleNotAvailableException or ActiveAssignmentExistsException);

                await using var check = new SkyShiftDbContext(options);
                Assert.Equal(1, await check.VehicleAssignments.CountAsync(x => x.UserId == user.Id));
                Assert.Equal(1, await check.Vehicles.CountAsync(v => (v.Id == first.Id || v.Id == second.Id) && v.Status == VehicleStatus.InUse));
            }
        }
        finally { await database.Database.EnsureDeletedAsync(); }
    }
}
