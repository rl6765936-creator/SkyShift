using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SkyShift.Domain.Entities;
using SkyShift.Domain.Enums;
using SkyShift.Infrastructure.Persistence;
using SkyShift.Infrastructure.Repositories;
using Xunit;

namespace SkyShift.Tests.Persistence;

public class CatalogModelTests
{
    private static SkyShiftDbContext SqlServerModelOnly() => new(new DbContextOptionsBuilder<SkyShiftDbContext>()
        .UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);

    [Fact]
    public void CatalogItemId_HasFilteredUniqueIndex_AndRestrictForeignKey()
    {
        using var context = SqlServerModelOnly();
        var vehicle = context.Model.FindEntityType(typeof(Vehicle))!;
        var index = Assert.Single(vehicle.GetIndexes(), i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "CatalogItemId" }));
        Assert.True(index.IsUnique);
        Assert.Equal("[CatalogItemId] IS NOT NULL", index.GetFilter());
        var foreignKey = Assert.Single(vehicle.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(VehicleCatalogItem));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.True(vehicle.FindProperty(nameof(Vehicle.CatalogItemId))!.IsNullable);
    }

    [Fact]
    public void Seed_HasDistinctRelativeImagePaths_CompleteSpecs_AndIllustrativeOnVisualInspiration()
    {
        using var context = SqlServerModelOnly();
        // Seed data is design-time metadata; the runtime (read-optimized) model does not keep it.
        var seed = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(VehicleCatalogItem))!.GetSeedData().ToList();

        Assert.Equal(22, seed.Count);
        Assert.Equal(seed.Count, seed.Select(s => s["Id"]).Distinct().Count());
        var images = seed.Select(s => (string)s["ImageUrl"]!).ToList();
        Assert.Equal(images.Count, images.Distinct().Count());
        Assert.All(images, image =>
        {
            Assert.Matches(@"^/catalog/car-\d{2}\.webp$", image);
            Assert.True(VehicleCatalogItem.IsRelativeAssetPath(image));
        });
        Assert.All(seed.Where(s => (CatalogIdentification)s["Identification"]! == CatalogIdentification.VisualInspiration), s =>
        {
            Assert.Equal(SpecificationAccuracy.Illustrative, (SpecificationAccuracy)s["SeatsAccuracy"]!);
            Assert.Equal(SpecificationAccuracy.Illustrative, (SpecificationAccuracy)s["MaxRoadSpeedAccuracy"]!);
        });
        Assert.All(seed, s => Assert.Contains("does not fly", (string)s["Description"]!));

        // Every item: seats, maximum road speed and both descriptions present.
        Assert.All(seed, s =>
        {
            Assert.InRange((int)s["Seats"]!, 1, 100);
            Assert.InRange((int)s["MaxRoadSpeedKmh"]!, 1, 600);
            Assert.True(Enum.IsDefined((SpecificationAccuracy)s["SeatsAccuracy"]!));
            Assert.True(Enum.IsDefined((SpecificationAccuracy)s["MaxRoadSpeedAccuracy"]!));
            Assert.False(string.IsNullOrWhiteSpace((string)s["Description"]!));
            Assert.False(string.IsNullOrWhiteSpace((string)s["DescriptionHe"]!));
            var illustrative = (SpecificationAccuracy)s["SeatsAccuracy"]! == SpecificationAccuracy.Illustrative
                || (SpecificationAccuracy)s["MaxRoadSpeedAccuracy"]! == SpecificationAccuracy.Illustrative;
            Assert.Equal(illustrative, ((string)s["Description"]!).Contains("not manufacturer figures"));
            Assert.Equal(illustrative, ((string)s["DescriptionHe"]!).Contains("אינם נתוני יצרן"));
        });

        // Only values confirmed from official sources are marked Verified.
        var verifiedSeats = seed.Where(s => (SpecificationAccuracy)s["SeatsAccuracy"]! == SpecificationAccuracy.Verified)
            .ToDictionary(s => (int)s["Id"]!, s => (int)s["Seats"]!);
        var verifiedSpeeds = seed.Where(s => (SpecificationAccuracy)s["MaxRoadSpeedAccuracy"]! == SpecificationAccuracy.Verified)
            .ToDictionary(s => (int)s["Id"]!, s => (int)s["MaxRoadSpeedKmh"]!);
        Assert.Equal(new Dictionary<int, int> { [6] = 4, [19] = 5, [21] = 4 }, verifiedSeats);
        Assert.Equal(new Dictionary<int, int> { [4] = 440, [21] = 160 }, verifiedSpeeds);
    }

    [Fact]
    public void CatalogMigration_OnlyAddsSchemaAndSeed_NeverRewritesExistingVehicles()
    {
        using var context = SqlServerModelOnly();
        Assert.False(context.Database.HasPendingModelChanges());

        var script = context.GetService<IMigrator>().GenerateScript(
            "20261006230426_AddFlightGuardsGenderAndVehicleDetails", "20261010203000_AddVehicleCatalog");

        Assert.Contains("CREATE TABLE [VehicleCatalogItems]", script);
        Assert.Contains("CREATE UNIQUE INDEX [IX_Vehicles_CatalogItemId] ON [Vehicles] ([CatalogItemId]) WHERE [CatalogItemId] IS NOT NULL", script);
        Assert.DoesNotContain("UPDATE [Vehicles]", script);
        Assert.DoesNotContain("DELETE FROM", script);
        Assert.DoesNotContain("DROP TABLE", script);
        Assert.DoesNotContain("DROP COLUMN", script);
    }

    [Fact]
    public void SpecificationMigration_UpdatesOnlyCatalogRows_BeforeMakingSpecsRequired()
    {
        using var context = SqlServerModelOnly();

        var script = context.GetService<IMigrator>().GenerateScript(
            "20261010203000_AddVehicleCatalog", "20261011090000_CompleteCatalogSpecifications");

        Assert.Contains("ALTER TABLE [Vehicles] ADD [SeatsAccuracy] nvarchar(20) NULL", script);
        Assert.Contains("ALTER TABLE [Vehicles] ADD [MaxRoadSpeedAccuracy] nvarchar(20) NULL", script);
        Assert.DoesNotContain("UPDATE [Vehicles]", script);
        Assert.DoesNotContain("DELETE FROM", script);
        Assert.DoesNotContain("DROP TABLE", script);
        Assert.DoesNotContain("DROP COLUMN", script);
        var lastUpdate = script.LastIndexOf("UPDATE [VehicleCatalogItems]", StringComparison.Ordinal);
        var firstRequired = script.IndexOf("ALTER TABLE [VehicleCatalogItems] ALTER COLUMN", StringComparison.Ordinal);
        Assert.True(lastUpdate >= 0 && firstRequired > lastUpdate);
        Assert.Contains("ALTER TABLE [VehicleCatalogItems] ALTER COLUMN [Seats] int NOT NULL", script);
        Assert.Contains("ALTER TABLE [VehicleCatalogItems] ALTER COLUMN [MaxRoadSpeedKmh] int NOT NULL", script);
    }

    [Fact]
    public async Task UnusedCatalog_ExcludesItemsUsedByAnyVehicle_EvenRetired()
    {
        var options = new DbContextOptionsBuilder<SkyShiftDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using (var seedContext = new SkyShiftDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync(); // applies the HasData catalog seed
            var catalog = await seedContext.VehicleCatalogItems.ToListAsync();
            var retired = Vehicle.Create(false, DateTime.UtcNow);
            retired.ApplyCatalogItem(catalog.Single(c => c.Id == 3));
            retired.Retire();
            var maintenance = Vehicle.Create(false, DateTime.UtcNow);
            maintenance.ApplyCatalogItem(catalog.Single(c => c.Id == 7));
            maintenance.SendToMaintenance();
            seedContext.Vehicles.AddRange(retired, maintenance, Vehicle.Create(false, DateTime.UtcNow));
            await seedContext.SaveChangesAsync();
        }

        await using var context = new SkyShiftDbContext(options);
        var repository = new VehicleCatalogRepository(context);
        var unused = await repository.GetUnusedAsync();

        Assert.Equal(20, unused.Count);
        Assert.DoesNotContain(unused, item => item.Id is 3 or 7);
        Assert.True(await repository.IsInUseAsync(3));
        Assert.False(await repository.IsInUseAsync(4));
        Assert.Null(await repository.GetByIdAsync(999));
    }
}
