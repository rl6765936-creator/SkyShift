using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.LoginUser;
using SkyShift.Application.UseCases.RegisterUser;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
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
    public async Task Registration_DuplicateEmail_ThrowsCodedConflict_AndDoesNotSave()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync("taken@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User.Create("Existing", "taken@example.test", 1, "hash"));
        var unit = new Mock<IUnitOfWork>();
        var error = await Assert.ThrowsAsync<EmailAlreadyRegisteredException>(() =>
            new RegisterUserUseCase(users.Object, Mock.Of<IPasswordHasher>(), unit.Object, Mapper()).ExecuteAsync(
                new RegisterUserRequest { Name = "Name", Email = "taken@example.test", Password = "secret123", AverageDailyTrips = 1 }));
        // Same English text as before, so existing clients keep working.
        Assert.Equal("Email 'taken@example.test' is already registered.", error.Message);
        Assert.IsAssignableFrom<InvalidOperationException>(error);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Login_UnknownEmailOrWrongPassword_ThrowsSameInvalidCredentials(bool userExists)
    {
        var users = new Mock<IUserRepository>();
        users.Setup(r => r.GetByEmailAsync("user@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userExists ? User.Create("User", "user@example.test", 1, "hash") : null);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            new LoginUserUseCase(users.Object, hasher.Object, Mapper()).ExecuteAsync(
                new LoginRequest { Email = "user@example.test", Password = "wrong-password" }));
        Assert.DoesNotContain("wrong-password", error.Message);
        Assert.DoesNotContain("user@example.test", error.Message);
    }

    [Fact]
    public void LoginResponse_ReturnsNullGender_ForLegacyAccount()
    {
        var response = Mapper().Map<LoginResponse>(User.Create("Legacy", "legacy@example.test", 1, "hash"));
        Assert.Null(response.Gender);
        Assert.Contains("\"Gender\":null", System.Text.Json.JsonSerializer.Serialize(response));
        var female = Mapper().Map<RegisterUserResponse>(User.Create("New", "new@example.test", 1, "hash", UserGender.Female));
        Assert.Contains("\"Gender\":\"Female\"", System.Text.Json.JsonSerializer.Serialize(female));
    }
}
