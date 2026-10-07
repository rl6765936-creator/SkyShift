using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.RegisterUser;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using Xunit;

namespace SkyShift.Tests.UseCases;

public class RegistrationAndVehicleMetadataTests
{
    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void MapperConfiguration_AllProfilesAreValid()
    {
        var configuration = new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void DependencyInjection_ResolvesMapper_AndMapsLoginGender()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(_ => { }, typeof(MappingProfile));
        using var provider = services.BuildServiceProvider();
        var response = provider.GetRequiredService<IMapper>().Map<LoginResponse>(
            User.Create("Test", "test@example.test", 1, "hash", UserGender.Male));
        Assert.Equal(UserGender.Male, response.Gender);
        Assert.Equal("Test", response.Name);
    }

    [Fact]
    public async Task Registration_PersistsGender_AndReturnsIt()
    {
        var users = new Mock<IUserRepository>();
        var password = new Mock<IPasswordHasher>();
        password.Setup(h => h.Hash("secret123")).Returns("hash");
        var unit = new Mock<IUnitOfWork>();
        var result = await new RegisterUserUseCase(users.Object, password.Object, unit.Object, Mapper()).ExecuteAsync(
            new RegisterUserRequest { Name = "Name", Email = "new@example.test", Password = "secret123", AverageDailyTrips = 2, Gender = UserGender.Female });
        Assert.Equal(UserGender.Female, result.Gender);
        users.Verify(r => r.AddAsync(It.Is<User>(u => u.Gender == UserGender.Female && u.PasswordHash == "hash" && u.Role == UserRole.User), It.IsAny<CancellationToken>()), Times.Once);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVehicle_MapsCatalogDetails_AndPreservesAvailableStatus()
    {
        var vehicles = new Mock<IVehicleRepository>();
        var unit = new Mock<IUnitOfWork>();
        var result = await new CreateVehicleUseCase(vehicles.Object, unit.Object, Mapper()).ExecuteAsync(
            new CreateVehicleRequest { ModelName = "SkyShift Azure", MaxSpeedKmh = 180, Seats = 4, ImageUrl = "/assets/azure.webp", IsPersonalExperimental = true });
        Assert.Equal("SkyShift Azure", result.ModelName);
        Assert.Equal(180, result.MaxSpeedKmh);
        Assert.Equal(4, result.Seats);
        Assert.Equal("/assets/azure.webp", result.ImageUrl);
        Assert.Equal(VehicleStatus.Available, result.Status);
        Assert.True(result.IsPersonalExperimental);
        vehicles.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Once);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
