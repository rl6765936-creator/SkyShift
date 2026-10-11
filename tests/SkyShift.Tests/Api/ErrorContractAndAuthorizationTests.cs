using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SkyShift.Api.Controllers;
using SkyShift.Api.Middleware;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.Api;

public class ErrorContractAndAuthorizationTests
{
    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Problem { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Problem = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Problem = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }

    private static async Task<(int Status, ProblemDetails Problem)> Handle(Exception exception, string language = "en")
    {
        var service = new CapturingProblemDetailsService();
        var handler = new SkyShiftExceptionHandler(service, NullLogger<SkyShiftExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptLanguage = language;
        await handler.TryHandleAsync(context, exception, CancellationToken.None);
        return (context.Response.StatusCode, service.Problem!);
    }

    [Theory]
    [InlineData(typeof(InvalidCredentialsException), 401, "invalid_credentials")]
    [InlineData(typeof(CatalogItemInUseException), 409, "catalog_item_in_use")]
    [InlineData(typeof(DuplicateFlightException), 409, "duplicate_flight")]
    public async Task CodedErrors_HaveStatusCodeAndBothLanguages(Type exceptionType, int status, string code)
    {
        var (actualStatus, problem) = await Handle((Exception)Activator.CreateInstance(exceptionType)!);

        Assert.Equal(status, actualStatus);
        Assert.Equal(code, problem.Extensions["code"]);
        Assert.False(string.IsNullOrWhiteSpace((string?)problem.Extensions["messageHe"]));
        Assert.False(string.IsNullOrWhiteSpace((string?)problem.Extensions["messageEn"]));
        Assert.NotEqual(problem.Extensions["messageHe"], problem.Extensions["messageEn"]);
    }

    [Fact]
    public async Task UnknownCatalogItem_Is404WithCode()
    {
        var (status, problem) = await Handle(new CatalogItemNotFoundException(999));
        Assert.Equal(404, status);
        Assert.Equal("catalog_item_not_found", problem.Extensions["code"]);
    }

    [Fact]
    public async Task DuplicateEmail_Keeps409AndOriginalEnglishText_AddsCode_AndHebrewDetailOnRequest()
    {
        var (status, problem) = await Handle(new EmailAlreadyRegisteredException("taken@example.test"));
        Assert.Equal(409, status);
        Assert.Equal("Email 'taken@example.test' is already registered.", problem.Detail);
        Assert.Equal("duplicate_email", problem.Extensions["code"]);

        var (_, hebrew) = await Handle(new EmailAlreadyRegisteredException("taken@example.test"), "he-IL");
        Assert.Equal(hebrew.Extensions["messageHe"], hebrew.Detail);
    }

    [Fact]
    public async Task UnexpectedErrors_NeverExposeInternalDetails()
    {
        var (status, problem) = await Handle(new Exception("SELECT * FROM Users; password=secret"));
        Assert.Equal(500, status);
        Assert.DoesNotContain("SELECT", problem.Detail);
        Assert.DoesNotContain("secret", problem.Detail);
        Assert.False(problem.Extensions.ContainsKey("code"));
    }

    [Fact]
    public void ManagerVehicleAndCatalogEndpoints_RequireManagerRole_WithNoAnonymousOverride()
    {
        var controller = typeof(ManagerVehiclesController);
        var authorize = Assert.Single(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
        Assert.Equal("Manager", authorize.Roles);

        var actions = controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.Contains(actions, a => a.Name == nameof(ManagerVehiclesController.GetCatalog));
        Assert.Contains(actions, a => a.Name == nameof(ManagerVehiclesController.Create));
        Assert.All(actions, a => Assert.Empty(a.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)));
        Assert.Empty(controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
    }
}
