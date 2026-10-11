using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyShift.Application.Abstractions;
using SkyShift.Application.DTOs;
using SkyShift.Application.Mapping;
using SkyShift.Application.UseCases.CreateVehicle;
using SkyShift.Application.UseCases.GetVehicleCatalog;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.UseCases;

public class CatalogUseCaseTests
{
    private static IMapper Mapper() => new MapperConfiguration(c => c.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    private static readonly VehicleCatalogItem MiniCooperE = VehicleCatalogItem.Create(
        21, "MINI", "Cooper E (3-door)", 4, SpecificationAccuracy.Verified, 160, SpecificationAccuracy.Verified,
        "English description", "תיאור בעברית", "/catalog/car-21.webp", CatalogIdentification.Identified);

    private static (CreateVehicleUseCase UseCase, Mock<IVehicleRepository> Vehicles, Mock<IUnitOfWork> Unit) Arrange(
        VehicleCatalogItem? item, bool inUse = false)
    {
        var catalog = new Mock<IVehicleCatalogRepository>();
        catalog.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => item is not null && item.Id == id ? item : null);
        catalog.Setup(r => r.IsInUseAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(inUse);
        var vehicles = new Mock<IVehicleRepository>();
        var unit = new Mock<IUnitOfWork>();
        return (new CreateVehicleUseCase(vehicles.Object, catalog.Object, unit.Object, Mapper()), vehicles, unit);
    }

    [Fact]
    public async Task Create_FromCatalog_CopiesAuthoritativeMetadata_AndKeepsAvailableStatus()
    {
        var (useCase, vehicles, unit) = Arrange(MiniCooperE);

        var result = await useCase.ExecuteAsync(new CreateVehicleRequest { CatalogItemId = 21, IsPersonalExperimental = true });

        Assert.Equal(21, result.CatalogItemId);
        Assert.Equal("MINI", result.Manufacturer);
        Assert.Equal("Cooper E (3-door)", result.ModelName);
        Assert.Equal(4, result.Seats);
        Assert.Equal(160, result.MaxRoadSpeedKmh);
        Assert.Equal(160, result.MaxSpeedKmh);
        Assert.Equal("/catalog/car-21.webp", result.ImageUrl);
        Assert.Equal("English description", result.Description);
        Assert.Equal("תיאור בעברית", result.DescriptionHe);
        Assert.Equal(CatalogIdentification.Identified, result.CatalogIdentification);
        Assert.Equal(SpecificationAccuracy.Verified, result.SeatsAccuracy);
        Assert.Equal(SpecificationAccuracy.Verified, result.MaxRoadSpeedAccuracy);
        Assert.Equal(VehicleStatus.Available, result.Status);
        Assert.True(result.IsPersonalExperimental);
        vehicles.Verify(r => r.AddAsync(It.Is<Vehicle>(v => v.CatalogItemId == 21), It.IsAny<CancellationToken>()), Times.Once);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_ClientSuppliedMetadata_IsIgnored_CatalogValuesWin()
    {
        // An older client still sending free-form metadata.
        var request = JsonSerializer.Deserialize<CreateVehicleRequest>(
            """{"catalogItemId":21,"isPersonalExperimental":false,"modelName":"Fake","maxSpeedKmh":999,"seats":50,"imageUrl":"C:\\photos\\car.jpg","description":"x"}""",
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var (useCase, _, _) = Arrange(MiniCooperE);

        var result = await useCase.ExecuteAsync(request);

        Assert.Equal("Cooper E (3-door)", result.ModelName);
        Assert.Equal(160, result.MaxSpeedKmh);
        Assert.Equal(4, result.Seats);
        Assert.Equal("/catalog/car-21.webp", result.ImageUrl);
        Assert.Equal(1, typeof(CreateVehicleRequest).GetProperties().Count(p => p.Name != nameof(CreateVehicleRequest.IsPersonalExperimental)));
    }

    [Fact]
    public async Task Create_UnknownCatalogItem_Throws404Type_AndDoesNotSave()
    {
        var (useCase, vehicles, unit) = Arrange(MiniCooperE);

        var error = await Assert.ThrowsAsync<CatalogItemNotFoundException>(() =>
            useCase.ExecuteAsync(new CreateVehicleRequest { CatalogItemId = 999 }));

        Assert.Equal(999, error.CatalogItemId);
        vehicles.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_AlreadyUsedCatalogItem_ThrowsConflict_AndDoesNotSave()
    {
        var (useCase, vehicles, unit) = Arrange(MiniCooperE, inUse: true);

        await Assert.ThrowsAsync<CatalogItemInUseException>(() =>
            useCase.ExecuteAsync(new CreateVehicleRequest { CatalogItemId = 21 }));

        vehicles.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        unit.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetCatalog_MapsUnusedItems_AlwaysWithSpecs_AndSeparateAccuracyFlags()
    {
        var illustrative = VehicleCatalogItem.Create(13, "Bestune", "Xiaoma (uncertain)",
            4, SpecificationAccuracy.Illustrative, 100, SpecificationAccuracy.Illustrative, "en", "he",
            "/catalog/car-13.webp", CatalogIdentification.VisualInspiration);
        var catalog = new Mock<IVehicleCatalogRepository>();
        catalog.Setup(r => r.GetUnusedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { illustrative, MiniCooperE });

        var items = await new GetVehicleCatalogUseCase(catalog.Object, Mapper()).ExecuteAsync();

        Assert.Equal(new[] { 13, 21 }, items.Select(i => i.Id));
        Assert.Equal(4, items[0].Seats);
        Assert.Equal(100, items[0].MaxRoadSpeedKmh);
        Assert.Equal(100, items[0].MaxSpeedKmh);
        Assert.Equal(SpecificationAccuracy.Illustrative, items[0].SeatsAccuracy);
        Assert.Equal(SpecificationAccuracy.Illustrative, items[0].MaxRoadSpeedAccuracy);
        Assert.Equal(CatalogIdentification.VisualInspiration, items[0].Identification);
        Assert.Equal(160, items[1].MaxRoadSpeedKmh);
        Assert.Equal(SpecificationAccuracy.Verified, items[1].MaxRoadSpeedAccuracy);
        var json = JsonSerializer.Serialize(items[0]);
        Assert.Contains("\"Identification\":\"VisualInspiration\"", json);
        Assert.Contains("\"SeatsAccuracy\":\"Illustrative\"", json);
        Assert.Contains("\"MaxRoadSpeedAccuracy\":\"Illustrative\"", json);
    }

    [Fact]
    public void LegacyVehicle_KeepsFreeFormSpeed_ButIsNotReportedAsRoadSpeed()
    {
        var legacy = Vehicle.Create(false, DateTime.UtcNow);
        legacy.SetCatalogDetails("Legacy", 180, 4, "/assets/legacy.webp");

        var response = Mapper().Map<VehicleResponse>(legacy);

        Assert.Null(response.CatalogItemId);
        Assert.Equal(180, response.MaxSpeedKmh);
        Assert.Null(response.MaxRoadSpeedKmh);
        Assert.Null(response.SeatsAccuracy);
        Assert.Null(response.MaxRoadSpeedAccuracy);
        Assert.Contains("\"Status\":0", JsonSerializer.Serialize(response));
    }
}
