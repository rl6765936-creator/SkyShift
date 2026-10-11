using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.RecordFlight;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.UseCases;

public class RecordFlightUseCaseTests
{
    private static readonly DateTime Now = new(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    private static (RecordFlightUseCase UseCase, Mock<IFlightRepository> Flights, Mock<IUnitOfWork> Unit) Arrange(bool duplicate = false, bool assigned = true)
    {
        var assignments = new Mock<IVehicleAssignmentRepository>();
        assignments.Setup(r => r.GetActiveForUserAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assigned ? VehicleAssignment.Create(7, 9, Now.AddDays(-10)) : null);
        var flights = new Mock<IFlightRepository>();
        flights.Setup(r => r.ExistsAsync(7, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(duplicate);
        var unit = new Mock<IUnitOfWork>();
        return (new RecordFlightUseCase(assignments.Object, flights.Object, unit.Object, new FixedClock()), flights, unit);
    }

    [Fact]
    public async Task ValidDate_SavesAuthenticatedUserAssignedVehicleAndServerTimestamp()
    {
        var (useCase, flights, unit) = Arrange();
        var date = Now.AddHours(-1);
        await useCase.ExecuteAsync(7, new RecordFlightRequest { Date = date });
        flights.Verify(r => r.AddAsync(It.Is<Flight>(f => f.UserId == 7 && f.VehicleId == 9 && f.Date == date && f.CreatedAt == Now), It.IsAny<CancellationToken>()), Times.Once);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(-11, "flight_outside_trial")]
    [InlineData(1, "flight_in_future")]
    [InlineData(30, "flight_outside_trial")]
    public async Task InvalidDate_DoesNotInsertOrSave(int days, string code)
    {
        var (useCase, flights, unit) = Arrange();
        var error = await Assert.ThrowsAsync<InvalidFlightDateException>(() => useCase.ExecuteAsync(7, new RecordFlightRequest { Date = Now.AddDays(days) }));
        Assert.Equal(code, error.Code);
        flights.Verify(r => r.AddAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Duplicate_DoesNotInsertOrSave()
    {
        var (useCase, flights, unit) = Arrange(duplicate: true);
        await Assert.ThrowsAsync<DuplicateFlightException>(() => useCase.ExecuteAsync(7, new RecordFlightRequest { Date = Now }));
        flights.Verify(r => r.AddAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoAssignment_DoesNotInsertOrSave()
    {
        var (useCase, flights, unit) = Arrange(assigned: false);
        await Assert.ThrowsAsync<NoActiveAssignmentException>(() => useCase.ExecuteAsync(7, new RecordFlightRequest { Date = Now }));
        flights.Verify(r => r.AddAsync(It.IsAny<Flight>(), It.IsAny<CancellationToken>()), Times.Never);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnspecifiedTimeZone_IsRejected()
    {
        var (useCase, _, unit) = Arrange();
        var error = await Assert.ThrowsAsync<InvalidFlightDateException>(() => useCase.ExecuteAsync(7, new RecordFlightRequest { Date = DateTime.SpecifyKind(Now, DateTimeKind.Unspecified) }));
        Assert.Equal("flight_time_zone_required", error.Code);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Boundary policy (UTC, inclusive): trial start and "now" are accepted;
    // one tick before the start, or one tick after now, is rejected.
    [Fact]
    public async Task Boundaries_TrialStartAndNowAccepted_OneTickOutsideRejected()
    {
        var trialStart = Now.AddDays(-10);

        var (atStart, _, _) = Arrange();
        await atStart.ExecuteAsync(7, new RecordFlightRequest { Date = trialStart });

        var (atNow, _, _) = Arrange();
        await atNow.ExecuteAsync(7, new RecordFlightRequest { Date = Now });

        var (beforeStart, _, unitBefore) = Arrange();
        Assert.Equal("flight_outside_trial", (await Assert.ThrowsAsync<InvalidFlightDateException>(() =>
            beforeStart.ExecuteAsync(7, new RecordFlightRequest { Date = trialStart.AddTicks(-1) }))).Code);
        unitBefore.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        var (afterNow, _, unitAfter) = Arrange();
        Assert.Equal("flight_in_future", (await Assert.ThrowsAsync<InvalidFlightDateException>(() =>
            afterNow.ExecuteAsync(7, new RecordFlightRequest { Date = Now.AddTicks(1) }))).Code);
        unitAfter.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
