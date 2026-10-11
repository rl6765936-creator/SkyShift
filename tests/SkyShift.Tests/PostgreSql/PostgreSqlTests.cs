using System.Data.Common;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.DeleteVehicle;
using SkyShift.Application.UseCases.RecordFlight;
using SkyShift.Application.UseCases.RegisterUser;
using SkyShift.Application.UseCases.SelectVehicle;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using SkyShift.Infrastructure;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using SkyShift.Infrastructure.Security;
using Xunit;

namespace SkyShift.Tests.PostgreSql;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_POSTGRES")))
            Skip = "Set SKYSHIFT_TEST_POSTGRES to a test PostgreSQL server with permission to create temporary databases.";
    }
}

/// <summary>
/// Real PostgreSQL checks for migrations, locking, error translation, duplicates, UTC dates and
/// rollback, each in its own temporary skyshift_tests_* database created through the PostgreSQL
/// migrations and dropped afterwards. Skipped unless SKYSHIFT_TEST_POSTGRES is set; never point it at
/// the deployed database.
/// </summary>
public class PostgreSqlTests
{
    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static DbContextOptions<SkyShiftDbContext> Options(params IInterceptor[] interceptors)
    {
        var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SKYSHIFT_TEST_POSTGRES"))
        {
            Database = "skyshift_tests_" + Guid.NewGuid().ToString("N")
        };
        var builder = new DbContextOptionsBuilder<SkyShiftDbContext>();
        DependencyInjection.UseProvider(builder, connection.ConnectionString, DatabaseProvider.PostgreSql);
        return builder.AddInterceptors(interceptors).Options;
    }

    private static DbContextOptions<SkyShiftDbContext> With(DbContextOptions<SkyShiftDbContext> options, IInterceptor interceptor) =>
        new DbContextOptionsBuilder<SkyShiftDbContext>(options).AddInterceptors(interceptor).Options;

    private static async Task<T> InFreshDatabaseAsync<T>(Func<DbContextOptions<SkyShiftDbContext>, SkyShiftDbContext, Task<T>> test)
    {
        var options = Options();
        await using var database = new SkyShiftDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            return await test(options, database);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await database.Database.EnsureDeletedAsync();
        }
    }

    private static Task InFreshDatabaseAsync(Func<DbContextOptions<SkyShiftDbContext>, SkyShiftDbContext, Task> test) =>
        InFreshDatabaseAsync<bool>(async (options, database) => { await test(options, database); return true; });

    private static async Task<Exception?> Capture(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception ex) { return ex; }
    }

    private static Task<Exception?> DeleteWith(DbContextOptions<SkyShiftDbContext> options, int vehicleId) => Capture(async () =>
    {
        await using var context = new SkyShiftDbContext(options);
        await new DeleteVehicleUseCase(new VehicleRepository(context), new UnitOfWork(context)).ExecuteAsync(vehicleId);
    });

    private static Task<Exception?> AssignWith(DbContextOptions<SkyShiftDbContext> options, int userId, int vehicleId) => Capture(async () =>
    {
        await using var context = new SkyShiftDbContext(options);
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

    // Holds the first transaction right after its locking vehicle read (FOR UPDATE), so the test
    // controls which side wins while the row lock is genuinely held.
    private sealed class HoldAfterVehicleLock : DbCommandInterceptor
    {
        private int _held;
        public TaskCompletionSource Locked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"Vehicles\"", StringComparison.Ordinal)
                && command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)
                && Interlocked.Exchange(ref _held, 1) == 0)
            {
                Locked.SetResult();
                await Release.Task;
            }
            return result;
        }
    }

    // ------------------------------------------------------------------ schema

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task Migrations_CreateTheSchema_With22CatalogItems_AndMatchTheModel() => InFreshDatabaseAsync(async (_, database) =>
    {
        Assert.Empty(await database.Database.GetPendingMigrationsAsync());
        Assert.False(database.Database.HasPendingModelChanges());
        Assert.Equal(22, await database.VehicleCatalogItems.CountAsync());
        Assert.Equal(22, (await new VehicleCatalogRepository(database).GetUnusedAsync()).Count);
        Assert.Contains(await database.VehicleCatalogItems.AsNoTracking().ToListAsync(), item => item.DescriptionHe.Contains("המדחפים"));
    });

    // ------------------------------------------------------------------ assignment and deletion

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task DeleteHoldsTheVehicleFirst_AssignmentWaits_ThenGets404_AndNothingIsLeftBehind() => InFreshDatabaseAsync(async (options, database) =>
    {
        var (user, vehicle) = await SeedPairAsync(database, "late");
        var hold = new HoldAfterVehicleLock();

        var deleting = DeleteWith(With(options, hold), vehicle.Id);
        await hold.Locked.Task.WaitAsync(TimeSpan.FromSeconds(30));
        var assigning = AssignWith(options, user.Id, vehicle.Id);
        await Task.Delay(750);
        Assert.False(assigning.IsCompleted); // blocked on the row lock, not racing ahead

        hold.Release.SetResult();
        Assert.Null(await deleting);
        Assert.IsType<VehicleNotFoundException>(await assigning);

        await using var check = new SkyShiftDbContext(options);
        Assert.False(await check.Vehicles.AnyAsync(v => v.Id == vehicle.Id));
        Assert.Equal(0, await check.VehicleAssignments.CountAsync(a => a.UserId == user.Id));

        // The losing transaction was rolled back and released its locks.
        var other = Vehicle.Create(false, DateTime.UtcNow);
        check.Vehicles.Add(other);
        await check.SaveChangesAsync();
        Assert.Null(await AssignWith(options, user.Id, other.Id).WaitAsync(TimeSpan.FromSeconds(30)));
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task AssignmentHoldsTheVehicleFirst_DeleteWaits_ThenGets409_AndAssignmentIsKept() => InFreshDatabaseAsync(async (options, database) =>
    {
        var (user, vehicle) = await SeedPairAsync(database, "early");
        var hold = new HoldAfterVehicleLock();

        var assigning = AssignWith(With(options, hold), user.Id, vehicle.Id);
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
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task ConcurrentDeleteAndAssignment_ExactlyOneWins_AndStateStaysConsistent() => InFreshDatabaseAsync(async (options, database) =>
    {
        var winners = new HashSet<string>();
        for (var round = 0; round < 20; round++)
        {
            var (user, vehicle) = await SeedPairAsync(database, $"racer{round}");
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<Exception?> After(Func<Task<Exception?>> action) { await gate.Task; return await action(); }

            var deleting = After(() => DeleteWith(options, vehicle.Id));
            var assigning = After(() => AssignWith(options, user.Id, vehicle.Id));
            gate.SetResult();
            var (deleteError, assignError) = (await deleting, await assigning);

            Assert.True(deleteError is null or VehicleDeletionNotAllowedException, $"Delete failed unexpectedly: {deleteError}");
            Assert.True(assignError is null or VehicleNotAvailableException or VehicleNotFoundException, $"Assign failed unexpectedly: {assignError}");
            Assert.True((deleteError is null) ^ (assignError is null), $"Round {round}: expected exactly one winner.");
            winners.Add(deleteError is null ? "delete" : "assign");

            await using var check = new SkyShiftDbContext(options);
            var stored = await check.Vehicles.AsNoTracking().SingleOrDefaultAsync(v => v.Id == vehicle.Id);
            var assignments = await check.VehicleAssignments.CountAsync(a => a.VehicleId == vehicle.Id);
            if (deleteError is null) { Assert.Null(stored); Assert.Equal(0, assignments); }
            else { Assert.Equal(VehicleStatus.InUse, stored!.Status); Assert.Equal(1, assignments); }
        }
        Assert.NotEmpty(winners);
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task ConcurrentAssignmentsOfOneVehicle_ExactlyOneUserGetsIt() => InFreshDatabaseAsync(async (options, database) =>
    {
        for (var round = 0; round < 5; round++)
        {
            var vehicle = Vehicle.Create(false, DateTime.UtcNow);
            var users = Enumerable.Range(0, 4).Select(i => User.Create($"U{i}", $"u{round}-{i}-{Guid.NewGuid():N}@example.test", 1, "hash")).ToList();
            database.Add(vehicle);
            database.AddRange(users);
            await database.SaveChangesAsync();

            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var attempts = users.Select(async u => { await gate.Task; return await AssignWith(options, u.Id, vehicle.Id); }).ToList();
            gate.SetResult();
            var errors = await Task.WhenAll(attempts);

            Assert.Single(errors, e => e is null);
            Assert.All(errors.Where(e => e is not null), e => Assert.IsType<VehicleNotAvailableException>(e));
            await using var check = new SkyShiftDbContext(options);
            Assert.Equal(1, await check.VehicleAssignments.CountAsync(a => a.VehicleId == vehicle.Id));
            Assert.Equal(VehicleStatus.InUse, (await check.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id)).Status);
        }
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task SameUserSelectingTwoVehiclesConcurrently_GetsOneAssignment_AndA409() => InFreshDatabaseAsync(async (options, database) =>
    {
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

            Assert.Single(errors, e => e is null);
            Assert.Single(errors, e => e is ActiveAssignmentExistsException or VehicleNotAvailableException);

            await using var check = new SkyShiftDbContext(options);
            Assert.Equal(1, await check.VehicleAssignments.CountAsync(x => x.UserId == user.Id));
            Assert.Equal(1, await check.Vehicles.CountAsync(v => (v.Id == first.Id || v.Id == second.Id) && v.Status == VehicleStatus.InUse));
        }
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task DeletionRules_HistoryAndForeignKeysAreRespected() => InFreshDatabaseAsync(async (options, database) =>
    {
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

        Assert.Null(await DeleteWith(options, unused.Id));
        Assert.Equal(VehicleDeletionNotAllowedException.NotAvailableCode, Assert.IsType<VehicleDeletionNotAllowedException>(await DeleteWith(options, inUse.Id)).Code);
        Assert.Equal(VehicleDeletionNotAllowedException.HasHistoryCode, Assert.IsType<VehicleDeletionNotAllowedException>(await DeleteWith(options, withHistory.Id)).Code);
        Assert.IsType<VehicleNotFoundException>(await DeleteWith(options, unused.Id));

        // Defence in depth: bypassing the history check, the foreign key (23503) still keeps the
        // history and is reported as a deletion conflict, not a 500.
        await using (var context = new SkyShiftDbContext(options))
        {
            context.Vehicles.Remove(await context.Vehicles.SingleAsync(v => v.Id == withHistory.Id));
            var fk = await Assert.ThrowsAsync<VehicleDeletionNotAllowedException>(() => new UnitOfWork(context).SaveChangesAsync());
            Assert.Equal(VehicleDeletionNotAllowedException.HasHistoryCode, fk.Code);
        }

        await using var check = new SkyShiftDbContext(options);
        Assert.Equal(2, await check.Vehicles.CountAsync()); // only the unused vehicle was deleted
        Assert.Equal(1, await check.VehicleAssignments.CountAsync());
        Assert.Equal(1, await check.Flights.CountAsync());
    });

    // ------------------------------------------------------------------ duplicates

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task ConcurrentRegistrations_WithTheSameEmailInAnyCase_CreateExactlyOneUser() => InFreshDatabaseAsync(async (options, database) =>
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Exception?> Register(string email) => await Capture(async () =>
        {
            await using var context = new SkyShiftDbContext(options);
            var useCase = new RegisterUserUseCase(new UserRepository(context), new PasswordHasher(), new UnitOfWork(context), Mapper());
            await gate.Task;
            await useCase.ExecuteAsync(new RegisterUserRequest { Name = "Same", Email = email, Password = "secret1", AverageDailyTrips = 2 });
        });

        var attempts = new[] { "same@example.test", "Same@Example.test", "SAME@EXAMPLE.TEST", "same@example.test" }.Select(Register).ToList();
        gate.SetResult();
        var errors = await Task.WhenAll(attempts);

        Assert.Single(errors, e => e is null);
        Assert.All(errors.Where(e => e is not null), e => Assert.IsType<EmailAlreadyRegisteredException>(e));
        Assert.Equal(1, await database.Users.CountAsync());
        // Lookups are case-insensitive too, as on SQL Server's default collation (login uses this).
        Assert.NotNull(await new UserRepository(database).GetByEmailAsync("sAmE@eXaMpLe.TeSt"));
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task ConcurrentCreates_ForSameCatalogItem_CreateExactlyOneVehicle() => InFreshDatabaseAsync(async (options, database) =>
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Exception?> Create() => await Capture(async () =>
        {
            await using var context = new SkyShiftDbContext(options);
            var useCase = new CreateVehicleUseCase(new VehicleRepository(context), new VehicleCatalogRepository(context), new UnitOfWork(context), Mapper());
            await gate.Task;
            await useCase.ExecuteAsync(new CreateVehicleRequest { CatalogItemId = 5 });
        });

        var attempts = Enumerable.Range(0, 4).Select(_ => Create()).ToList();
        gate.SetResult();
        var errors = await Task.WhenAll(attempts);

        Assert.Single(errors, e => e is null);
        Assert.All(errors.Where(e => e is not null), e => Assert.IsType<CatalogItemInUseException>(e));
        Assert.Equal(1, await database.Vehicles.CountAsync(v => v.CatalogItemId == 5));
        Assert.DoesNotContain(await new VehicleCatalogRepository(database).GetUnusedAsync(), item => item.Id == 5);
        Assert.Equal(21, (await new VehicleCatalogRepository(database).GetUnusedAsync()).Count);
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task ConcurrentDuplicateFlightReports_OnlyOneCommits() => InFreshDatabaseAsync(async (options, database) =>
    {
        var (user, vehicle) = await SeedPairAsync(database, "pilot");
        var date = DateTime.UtcNow.AddHours(-1);
        database.VehicleAssignments.Add(VehicleAssignment.Create(user.Id, vehicle.Id, date.AddDays(-1)));
        await database.SaveChangesAsync();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<Exception?> Report() => await Capture(async () =>
        {
            await using var context = new SkyShiftDbContext(options);
            await gate.Task;
            await new RecordFlightUseCase(new VehicleAssignmentRepository(context), new FlightRepository(context), new UnitOfWork(context), TimeProvider.System)
                .ExecuteAsync(user.Id, new RecordFlightRequest { Date = date });
        });

        var attempts = Enumerable.Range(0, 3).Select(_ => Report()).ToList();
        gate.SetResult();
        var errors = await Task.WhenAll(attempts);

        Assert.Single(errors, e => e is null);
        Assert.All(errors.Where(e => e is not null), e => Assert.IsType<DuplicateFlightException>(e));
        Assert.Equal(1, await database.Flights.CountAsync());
    });

    // ------------------------------------------------------------------ UTC dates

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task Dates_AreStoredAndReturnedAsUtc_AndFiltersAcceptAnyKind() => InFreshDatabaseAsync(async (options, database) =>
    {
        var (user, vehicle) = await SeedPairAsync(database, "clock");
        var start = new DateTime(2026, 10, 1, 21, 30, 15, 123, DateTimeKind.Utc).AddTicks(4560); // microsecond precision
        var flightDate = new DateTime(2026, 10, 2, 23, 45, 0, DateTimeKind.Utc);
        database.VehicleAssignments.Add(VehicleAssignment.Create(user.Id, vehicle.Id, start));
        database.Flights.Add(Flight.Create(user.Id, vehicle.Id, flightDate, DateTime.UtcNow));
        await database.SaveChangesAsync();

        await using var check = new SkyShiftDbContext(options);
        var assignment = await check.VehicleAssignments.AsNoTracking().SingleAsync(a => a.UserId == user.Id);
        Assert.Equal(DateTimeKind.Utc, assignment.StartDate.Kind);
        Assert.Equal(start, assignment.StartDate);
        Assert.Equal(start.AddMonths(1), assignment.EndDate);

        var flight = await check.Flights.AsNoTracking().SingleAsync();
        Assert.Equal(DateTimeKind.Utc, flight.Date.Kind);
        Assert.Equal(flightDate, flight.Date);

        // The database default (now()) and an entity's own CreatedAt are both UTC instants.
        var created = await check.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id);
        Assert.Equal(DateTimeKind.Utc, created.CreatedAt.Kind);
        Assert.InRange(created.CreatedAt, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(1));
        await check.Database.ExecuteSqlRawAsync("INSERT INTO \"Flights\" (\"UserId\", \"Date\") VALUES ({0}, {1})", user.Id, flightDate.AddDays(1));
        var defaulted = await check.Flights.AsNoTracking().SingleAsync(f => f.Date == flightDate.AddDays(1));
        Assert.InRange(defaulted.CreatedAt, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(1));

        // Manager filters may arrive as Unspecified (date-only) or Local (bound "...Z" values);
        // both are compared as UTC instead of failing.
        var repository = new FlightRepository(check);
        var unspecified = DateTime.SpecifyKind(flightDate.AddMinutes(-1), DateTimeKind.Unspecified);
        Assert.Equal(2, (await repository.GetAllAsync(null, null, unspecified, null)).Count);
        Assert.Single(await repository.GetAllAsync(null, null, flightDate.AddMinutes(-1).ToLocalTime(), flightDate.AddMinutes(1).ToLocalTime()));
        Assert.True(await repository.ExistsAsync(user.Id, flightDate));

        // The batched dashboard count runs on PostgreSQL as well.
        var counts = await repository.GetCompletedCountsForAssignmentsAsync(new[] { assignment.Id });
        Assert.Equal(2, counts[assignment.Id]);
    });

    // ------------------------------------------------------------------ rollback and concurrency token

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task FailedSave_RollsBackTheWholeTransaction_AndTheScopeStaysUsable() => InFreshDatabaseAsync(async (options, database) =>
    {
        var (user, vehicle) = await SeedPairAsync(database, "rollback");

        await using (var context = new SkyShiftDbContext(options))
        {
            // Same shape as SelectVehicle (lock, status change, assignment insert), but the insert
            // references a missing user, so the whole unit must fail and leave nothing behind.
            var vehicles = new VehicleRepository(context);
            var locked = await vehicles.GetByIdAsync(vehicle.Id);
            locked!.Assign();
            await vehicles.UpdateAsync(locked);
            await new VehicleAssignmentRepository(context).AddAsync(VehicleAssignment.Create(user.Id + 1000, vehicle.Id, DateTime.UtcNow));
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => new UnitOfWork(context).SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(failure.InnerException).SqlState);
        }

        await using var check = new SkyShiftDbContext(options);
        Assert.Equal(VehicleStatus.Available, (await check.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id)).Status);
        Assert.Equal(0, await check.VehicleAssignments.CountAsync());
        // The row lock was released by the rollback: the real assignment goes through at once.
        Assert.Null(await AssignWith(options, user.Id, vehicle.Id).WaitAsync(TimeSpan.FromSeconds(30)));
    });

    [PostgreSqlFact]
    [Trait("Category", "PostgreSql")]
    public Task StaleVehicleUpdate_IsRejectedByTheXminToken_AsVehicleNotAvailable() => InFreshDatabaseAsync(async (options, database) =>
    {
        var (_, vehicle) = await SeedPairAsync(database, "stale");
        await using var first = new SkyShiftDbContext(options);
        await using var second = new SkyShiftDbContext(options);
        var a = await new VehicleRepository(first).GetByIdForManagementAsync(vehicle.Id);
        var b = await new VehicleRepository(second).GetByIdForManagementAsync(vehicle.Id);

        a!.SendToMaintenance();
        await new VehicleRepository(first).UpdateStatusAsync(a);
        await new UnitOfWork(first).SaveChangesAsync();

        b!.Retire();
        await new VehicleRepository(second).UpdateStatusAsync(b);
        await Assert.ThrowsAsync<VehicleNotAvailableException>(() => new UnitOfWork(second).SaveChangesAsync());

        await using var check = new SkyShiftDbContext(options);
        Assert.Equal(VehicleStatus.Maintenance, (await check.Vehicles.AsNoTracking().SingleAsync(v => v.Id == vehicle.Id)).Status);
    });
}
