using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SkyShift.Api.Middleware;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.UseCases.SelectVehicle;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.Api;

/// <summary>
/// Expected business outcomes map to specific 4xx codes with bilingual messages;
/// unexpected exceptions (including bare InvalidOperationException and
/// KeyNotFoundException) stay 500. Log levels follow the outcome category.
/// </summary>
public class ExceptionClassificationTests
{
    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Problem { get; private set; }
        public ValueTask WriteAsync(ProblemDetailsContext context) { Problem = context.ProblemDetails; return ValueTask.CompletedTask; }
        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context) { Problem = context.ProblemDetails; return ValueTask.FromResult(true); }
    }

    private sealed class CapturingLogger : ILogger<SkyShiftExceptionHandler>
    {
        public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception), exception));
    }

    private static async Task<(int Status, ProblemDetails Problem, CapturingLogger Log)> Handle(Exception exception, string language = "en")
    {
        var service = new CapturingProblemDetailsService();
        var logger = new CapturingLogger();
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptLanguage = language;
        await new SkyShiftExceptionHandler(service, logger).TryHandleAsync(context, exception, CancellationToken.None);
        return (context.Response.StatusCode, service.Problem!, logger);
    }

    public static TheoryData<Exception, int, string> ExpectedOutcomes => new()
    {
        { new ActiveAssignmentExistsException(), 409, "active_assignment_exists" },
        { new NoActiveAssignmentException(), 409, "no_active_assignment" },
        { new VehicleNotAvailableException(5, VehicleStatus.InUse), 409, "vehicle_not_available" },
        { new InvalidVehicleStatusTransitionException(5, VehicleStatus.Retired, VehicleStatus.Available), 409, "invalid_status_transition" },
        { new VehicleNotFoundException(5), 404, "vehicle_not_found" },
        { new UserNotFoundException(5), 404, "user_not_found" },
        { VehicleDeletionNotAllowedException.NotAvailable(5, VehicleStatus.InUse), 409, "vehicle_delete_not_available" },
        { VehicleDeletionNotAllowedException.HasHistory(5), 409, "vehicle_delete_has_history" },
        { VehicleDeletionNotAllowedException.ChangedConcurrently(5), 409, "vehicle_delete_conflict" },
    };

    [Theory]
    [MemberData(nameof(ExpectedOutcomes))]
    public async Task ExpectedOutcomes_HaveSpecificStatus_Code_AndBothLanguages(Exception exception, int status, string code)
    {
        var (actual, problem, _) = await Handle(exception);

        Assert.Equal(status, actual);
        Assert.Equal(code, problem.Extensions["code"]);
        Assert.Equal(exception.Message, problem.Detail); // original English detail is unchanged for English clients
        var he = Assert.IsType<string>(problem.Extensions["messageHe"]);
        var en = Assert.IsType<string>(problem.Extensions["messageEn"]);
        Assert.False(string.IsNullOrWhiteSpace(he));
        Assert.False(string.IsNullOrWhiteSpace(en));
        Assert.NotEqual(he, en);

        var (_, hebrew, _) = await Handle(exception, "he-IL");
        Assert.Equal(he, hebrew.Detail);
    }

    [Fact]
    public async Task BareInvalidOperationException_IsAServerError_NotA409()
    {
        var (status, problem, log) = await Handle(new InvalidOperationException("Sequence contains no elements"));
        Assert.Equal(500, status);
        Assert.False(problem.Extensions.ContainsKey("code"));
        Assert.DoesNotContain("Sequence", problem.Detail);
        Assert.Equal(LogLevel.Error, Assert.Single(log.Entries).Level);
    }

    [Fact]
    public async Task BareKeyNotFoundException_IsAServerError_NotA404()
    {
        var (status, _, log) = await Handle(new KeyNotFoundException("The given key 'x' was not present in the dictionary."));
        Assert.Equal(500, status);
        Assert.Equal(LogLevel.Error, Assert.Single(log.Entries).Level);
    }

    [Fact]
    public async Task LogLevels_FollowOutcomeCategory()
    {
        Assert.Equal(LogLevel.Information, Assert.Single((await Handle(new InvalidFlightDateException("flight_in_future", "Flight date cannot be in the future."))).Log.Entries).Level);
        Assert.Equal(LogLevel.Information, Assert.Single((await Handle(new VehicleNotFoundException(9))).Log.Entries).Level);
        Assert.Equal(LogLevel.Warning, Assert.Single((await Handle(new VehicleNotAvailableException(9, VehicleStatus.InUse))).Log.Entries).Level);
        Assert.Equal(LogLevel.Warning, Assert.Single((await Handle(new InvalidCredentialsException())).Log.Entries).Level);
        Assert.Equal(LogLevel.Error, Assert.Single((await Handle(new Exception("boom"))).Log.Entries).Level);

        // Expected outcomes are logged without a stack trace; failures keep the exception.
        Assert.Null(Assert.Single((await Handle(new VehicleNotAvailableException(9, VehicleStatus.InUse))).Log.Entries).Exception);
        Assert.NotNull(Assert.Single((await Handle(new Exception("boom"))).Log.Entries).Exception);
    }

    [Fact]
    public async Task DuplicateEmailConflict_LogsWarningWithoutTheEmailAddress()
    {
        var entry = Assert.Single((await Handle(new EmailAlreadyRegisteredException("private@example.test"))).Log.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.DoesNotContain("private@example.test", entry.Message);
    }

    [Fact]
    public async Task SelectVehicle_WithActiveAssignment_ThrowsSpecificConflict_AndDoesNotSave()
    {
        var assignments = new Mock<IVehicleAssignmentRepository>();
        assignments.Setup(r => r.GetActiveForUserAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VehicleAssignment.Create(1, 2, DateTime.UtcNow.AddDays(-1)));
        var vehicles = new Mock<IVehicleRepository>();
        var unit = new Mock<IUnitOfWork>();

        await Assert.ThrowsAsync<ActiveAssignmentExistsException>(() =>
            new SelectVehicleUseCase(vehicles.Object, assignments.Object, unit.Object).ExecuteAsync(1, new SelectVehicleRequest { VehicleId = 2 }));
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SelectVehicle_UnknownVehicle_ThrowsVehicleNotFound()
    {
        var assignments = new Mock<IVehicleAssignmentRepository>();
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Vehicle?)null);

        var error = await Assert.ThrowsAsync<VehicleNotFoundException>(() =>
            new SelectVehicleUseCase(vehicles.Object, assignments.Object, new Mock<IUnitOfWork>().Object).ExecuteAsync(1, new SelectVehicleRequest { VehicleId = 99 }));
        Assert.Equal(99, error.VehicleId);
    }
}
