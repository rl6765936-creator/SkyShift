using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SkyShift.Api.Authentication;
using SkyShift.Api.Controllers;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.GetManagerVehicle;
using SkyShift.Application.UseCases.GetUserProfile;
using SkyShift.Application.UseCases.RegisterUser;
using SkyShift.Application.UseCases.SelectVehicle;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.Api;

/// <summary>
/// 201 Location headers are generated from named GET routes that really exist,
/// and the routes carry the intended authorization. Uses the real ASP.NET Core
/// routing (controller endpoints + LinkGenerator) without starting a server.
/// </summary>
public class RoutesAndLocationTests
{
    private static readonly Lazy<WebApplication> RoutingApp = new(() =>
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        var app = builder.Build();
        // UseEndpoints registers the controller endpoints with routing (normally done
        // when the host starts), so LinkGenerator and the endpoint list are populated.
        app.UseRouting();
#pragma warning disable ASP0014 // explicit UseEndpoints is intended: the host is never started in this test
        app.UseEndpoints(endpoints => endpoints.MapControllers());
#pragma warning restore ASP0014
        return app;
    });

    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static T WithId<T>(T entity, int id) where T : class
    {
        typeof(T).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }

    private static RouteEndpoint Endpoint(string method, string pattern) =>
        Assert.Single(RoutingApp.Value.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>(),
            e => e.RoutePattern.RawText == pattern
                 && e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) == true);

    private static string? PathFor(CreatedAtRouteResult created) =>
        RoutingApp.Value.Services.GetRequiredService<LinkGenerator>().GetPathByName(created.RouteName!, created.RouteValues);

    private static ClaimsPrincipal Caller(int id, params string[] roles) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }.Concat(roles.Select(r => new Claim(ClaimTypes.Role, r))), "Test"));

    [Fact]
    public async Task CreateVehicle_Location_PointsToAnExistingManagerOnlyGet()
    {
        var item = VehicleCatalogItem.Create(3, "Maker", "Model", 4, SpecificationAccuracy.Verified, 200, SpecificationAccuracy.Verified,
            "Description", "תיאור", "/catalog/car-03.webp", CatalogIdentification.Identified);
        var catalog = new Mock<IVehicleCatalogRepository>();
        catalog.Setup(c => c.GetByIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(item);
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(v => v.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
            .Callback<Vehicle, CancellationToken>((v, _) => WithId(v, 41));
        var controller = new ManagerVehiclesController(
            new CreateVehicleUseCase(vehicles.Object, catalog.Object, new Mock<IUnitOfWork>().Object, Mapper()),
            null!, null!, null!, null!, null!);

        var result = await controller.Create(new CreateVehicleRequest { CatalogItemId = 3 }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Equal("/api/manager/vehicles/41", PathFor(created));
        var get = Endpoint("GET", "api/manager/vehicles/{id:int}");
        Assert.Contains(get.Metadata.GetOrderedMetadata<AuthorizeAttribute>(), a => a.Roles == "Manager");
        Assert.Empty(get.Metadata.GetOrderedMetadata<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task Register_Location_PointsToAnExistingAuthorizedUserGet()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => WithId(u, 17));
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        var jwt = new JwtTokenService(Options.Create(new JwtOptions
        { Issuer = "test", Audience = "test", SigningKey = new string('k', 64), ExpirationMinutes = 5 }));
        var controller = new AuthController(
            new RegisterUserUseCase(users.Object, hasher.Object, new Mock<IUnitOfWork>().Object, Mapper()), null!, jwt);

        var result = await controller.Register(new RegisterUserRequest
        { Name = "New", Email = "new@example.test", Password = "secret1", AverageDailyTrips = 2 }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtRouteResult>(result.Result);
        Assert.Equal("/api/users/17", PathFor(created));
        var get = Endpoint("GET", "api/users/{id:int}");
        Assert.NotEmpty(get.Metadata.GetOrderedMetadata<AuthorizeAttribute>());
        Assert.Empty(get.Metadata.GetOrderedMetadata<AllowAnonymousAttribute>());
    }

    [Fact]
    public void DeleteVehicle_IsAManagerOnlyRoute()
    {
        var delete = Endpoint("DELETE", "api/manager/vehicles/{id:int}");
        Assert.Contains(delete.Metadata.GetOrderedMetadata<AuthorizeAttribute>(), a => a.Roles == "Manager");
        Assert.Empty(delete.Metadata.GetOrderedMetadata<AllowAnonymousAttribute>());
    }

    [Fact]
    public void Assignment_HasResourceRoute_AndKeepsTheSelectAliasForTheCurrentClient()
    {
        var resource = Endpoint("POST", "api/vehicles/{id:int:min(1)}/assignments");
        var alias = Endpoint("POST", "api/vehicles/select");
        Assert.NotEmpty(resource.Metadata.GetOrderedMetadata<AuthorizeAttribute>());
        Assert.NotEmpty(alias.Metadata.GetOrderedMetadata<AuthorizeAttribute>());
    }

    [Fact]
    public async Task AssignmentRoute_UsesTheRouteVehicleId_ThroughTheSameUseCase()
    {
        var vehicle = WithId(Vehicle.Create(false, DateTime.UtcNow), 7);
        var vehicles = new Mock<IVehicleRepository>();
        vehicles.Setup(v => v.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(vehicle);
        var assignments = new Mock<IVehicleAssignmentRepository>();
        var controller = new VehiclesController(null!, new SelectVehicleUseCase(vehicles.Object, assignments.Object, new Mock<IUnitOfWork>().Object), null!)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = Caller(3) } } };

        var result = await controller.CreateAssignment(7, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(VehicleStatus.InUse, vehicle.Status);
        assignments.Verify(a => a.AddAsync(It.Is<VehicleAssignment>(x => x.UserId == 3 && x.VehicleId == 7), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(5, 5, false, true)]  // own profile
    [InlineData(5, 6, false, false)] // another user's profile: hidden as 404
    [InlineData(5, 6, true, true)]   // manager may read any profile
    public async Task UserProfile_IsVisibleToSelfAndManagersOnly(int requested, int caller, bool manager, bool visible)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(WithId(User.Create("Five", "five@example.test", 2, "hash"), 5));
        var controller = new UsersController(new GetUserProfileUseCase(users.Object, Mapper()))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = manager ? Caller(caller, "Manager") : Caller(caller) } } };

        if (visible)
        {
            var ok = Assert.IsType<OkObjectResult>((await controller.GetById(requested, CancellationToken.None)).Result);
            var profile = Assert.IsType<RegisterUserResponse>(ok.Value);
            Assert.Equal("five@example.test", profile.Email);
        }
        else
        {
            await Assert.ThrowsAsync<UserNotFoundException>(() => controller.GetById(requested, CancellationToken.None));
            users.Verify(u => u.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task UserProfile_MissingUser_IsNotFound_EvenForManagers()
    {
        var users = new Mock<IUserRepository>();
        await Assert.ThrowsAsync<UserNotFoundException>(() =>
            new GetUserProfileUseCase(users.Object, Mapper()).ExecuteAsync(404, 1, callerIsManager: true));
    }

    [Fact]
    public async Task ManagerVehicle_GetById_ReadsUntracked_OrIsNotFound()
    {
        var vehicles = new Mock<IVehicleRepository>(MockBehavior.Strict);
        vehicles.Setup(v => v.GetByIdForReadAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(WithId(Vehicle.Create(true, DateTime.UtcNow), 8));
        vehicles.Setup(v => v.GetByIdForReadAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Vehicle?)null);
        var useCase = new GetManagerVehicleUseCase(vehicles.Object, Mapper());

        Assert.Equal(8, (await useCase.ExecuteAsync(8)).Id);
        await Assert.ThrowsAsync<VehicleNotFoundException>(() => useCase.ExecuteAsync(9));
    }
}
