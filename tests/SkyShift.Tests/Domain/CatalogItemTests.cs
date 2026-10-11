using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Domain.Exceptions;
using Xunit;

namespace SkyShift.Tests.Domain;

public class CatalogItemTests
{
    private static VehicleCatalogItem Item(int id = 4, int seats = 2, int speed = 440,
        SpecificationAccuracy seatsAccuracy = SpecificationAccuracy.Illustrative,
        SpecificationAccuracy speedAccuracy = SpecificationAccuracy.Verified,
        CatalogIdentification identification = CatalogIdentification.Identified, string image = "/catalog/car-04.webp",
        string description = "en", string descriptionHe = "he") =>
        VehicleCatalogItem.Create(id, "Bugatti", "Chiron Super Sport", seats, seatsAccuracy, speed, speedAccuracy,
            description, descriptionHe, image, identification);

    [Theory]
    [InlineData("C:\\Users\\me\\car.webp")]
    [InlineData("catalog/car-01.webp")]
    [InlineData("//cdn.example/car.webp")]
    [InlineData("https://cdn.example/car.webp")]
    [InlineData("/catalog/car 01.webp")]
    [InlineData("data:image/webp;base64,AAAA")]
    public void ImagePath_MustBeRootRelativeFrontendAsset(string image)
    {
        Assert.Throws<InvalidVehicleCatalogException>(() => Item(image: image));
    }

    [Fact]
    public void VisualInspiration_CanOnlyCarryIllustrativeSpecifications()
    {
        Assert.Throws<InvalidVehicleCatalogException>(() =>
            Item(identification: CatalogIdentification.VisualInspiration, speedAccuracy: SpecificationAccuracy.Verified));
        Assert.Throws<InvalidVehicleCatalogException>(() =>
            Item(identification: CatalogIdentification.VisualInspiration,
                seatsAccuracy: SpecificationAccuracy.Verified, speedAccuracy: SpecificationAccuracy.Illustrative));

        var item = Item(identification: CatalogIdentification.VisualInspiration, speed: 350,
            speedAccuracy: SpecificationAccuracy.Illustrative);
        Assert.Equal(350, item.MaxRoadSpeedKmh);
        Assert.Equal(SpecificationAccuracy.Illustrative, item.MaxRoadSpeedAccuracy);
        Assert.Equal(SpecificationAccuracy.Illustrative, item.SeatsAccuracy);
    }

    [Theory]
    [InlineData(0, 440)]
    [InlineData(101, 440)]
    [InlineData(2, 0)]
    [InlineData(2, 601)]
    public void SeatsAndSpeed_AreRequiredAndInRange(int seats, int speed)
    {
        Assert.Throws<InvalidVehicleCatalogException>(() => Item(seats: seats, speed: speed));
    }

    [Theory]
    [InlineData("", "he")]
    [InlineData("en", " ")]
    public void BothDescriptions_AreRequired(string description, string descriptionHe)
    {
        Assert.Throws<InvalidVehicleCatalogException>(() => Item(description: description, descriptionHe: descriptionHe));
    }

    [Fact]
    public void UndefinedAccuracy_IsRejected()
    {
        Assert.Throws<InvalidVehicleCatalogException>(() => Item(seatsAccuracy: (SpecificationAccuracy)9));
    }

    [Fact]
    public void ApplyCatalogItem_CopiesMetadataAndAccuracy_OnceOnly_AndLocksManualEdits()
    {
        var vehicle = Vehicle.Create(isPersonalExperimental: false, DateTime.UtcNow);

        vehicle.ApplyCatalogItem(Item());

        Assert.Equal(4, vehicle.CatalogItemId);
        Assert.Equal("Bugatti", vehicle.Manufacturer);
        Assert.Equal(440, vehicle.MaxSpeedKmh);
        Assert.Equal(SpecificationAccuracy.Verified, vehicle.MaxRoadSpeedAccuracy);
        Assert.Equal(2, vehicle.Seats);
        Assert.Equal(SpecificationAccuracy.Illustrative, vehicle.SeatsAccuracy);
        Assert.Equal(VehicleStatus.Available, vehicle.Status);
        Assert.Throws<InvalidVehicleCatalogException>(() => vehicle.ApplyCatalogItem(Item(id: 5, image: "/catalog/car-05.webp")));
        Assert.Throws<InvalidVehicleCatalogException>(() => vehicle.SetCatalogDetails("Other", 100, 2, "/x.webp"));
    }

    [Fact]
    public void ApplyCatalogItem_NeverOverwritesExistingLegacyDetails()
    {
        var legacy = Vehicle.Create(false, DateTime.UtcNow);
        legacy.SetCatalogDetails("Legacy name", 180, 4, "/assets/legacy.webp");

        Assert.Throws<InvalidVehicleCatalogException>(() => legacy.ApplyCatalogItem(Item()));
        Assert.Null(legacy.CatalogItemId);
        Assert.Equal("Legacy name", legacy.ModelName);
        Assert.Null(legacy.SeatsAccuracy);
        Assert.Null(legacy.MaxRoadSpeedAccuracy);
    }

    [Fact]
    public void RetiredCatalogVehicle_KeepsItsCatalogLink()
    {
        var vehicle = Vehicle.Create(false, DateTime.UtcNow);
        vehicle.ApplyCatalogItem(Item());
        vehicle.Retire();
        Assert.Equal(4, vehicle.CatalogItemId);
    }
}
