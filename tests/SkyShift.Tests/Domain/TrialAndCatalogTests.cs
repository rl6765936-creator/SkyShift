using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SkyShift.Application.DTOs;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.Domain;

public class TrialAndCatalogTests
{
    [Fact]
    public void Trial_BothBoundariesAreInclusive_OneTickOutsideIsRejected()
    {
        var start = new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc);
        var assignment = VehicleAssignment.Create(1, 2, start);
        Assert.Equal(start.AddMonths(1), assignment.EndDate);
        assignment.ValidateFlightDate(start, assignment.EndDate);
        assignment.ValidateFlightDate(assignment.EndDate, assignment.EndDate);
        Assert.Throws<InvalidFlightDateException>(() => assignment.ValidateFlightDate(start.AddTicks(-1), assignment.EndDate));
        Assert.Throws<InvalidFlightDateException>(() => assignment.ValidateFlightDate(assignment.EndDate.AddTicks(1), assignment.EndDate.AddDays(1)));
    }

    [Fact]
    public void Gender_LegacyAccountsRemainUnknown_ChosenGenderIsStored()
    {
        Assert.Null(User.Create("Legacy", "legacy@example.test", 1, "hash").Gender);
        Assert.Equal(UserGender.Male, User.Create("New", "new@example.test", 1, "hash", UserGender.Male).Gender);
        Assert.Throws<ArgumentOutOfRangeException>(() => User.Create("Invalid", "bad@example.test", 1, "hash", (UserGender)9));
    }

    [Fact]
    public void Registration_AcceptsGenderName_RejectsUndefinedEnum()
    {
        var request = JsonSerializer.Deserialize<RegisterUserRequest>("{\"Gender\":\"female\"}")!;
        Assert.Equal(UserGender.Female, request.Gender);
        request.Name = "Example"; request.Email = "test@example.test"; request.Password = "123456"; request.AverageDailyTrips = 1;
        request.Gender = (UserGender)42;
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true));
    }

    [Fact]
    public void Catalog_LegacyVehicleWorks_NewDetailsArePreserved()
    {
        var vehicle = Vehicle.Create(false, DateTime.UtcNow);
        Assert.Null(vehicle.ModelName);
        vehicle.SetCatalogDetails("  SkyShift Azure  ", 180, 4, "/assets/azure.webp");
        Assert.Equal("SkyShift Azure", vehicle.ModelName);
        Assert.Equal(180, vehicle.MaxSpeedKmh);
        Assert.Equal(4, vehicle.Seats);
        Assert.Equal("/assets/azure.webp", vehicle.ImageUrl);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//external.example/car.jpg")]
    [InlineData("/bad path.jpg")]
    public void Catalog_InvalidImageUrl_IsRejected(string image)
    {
        Assert.Throws<InvalidVehicleCatalogException>(() => Vehicle.Create(false, DateTime.UtcNow).SetCatalogDetails("Model", 180, 4, image));
    }
}
