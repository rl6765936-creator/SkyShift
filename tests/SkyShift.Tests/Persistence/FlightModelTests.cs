using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SkyShift.Domain.Entities;
using SkyShift.Infrastructure.Persistence;
using Xunit;

namespace SkyShift.Tests.Persistence;

public class FlightModelTests
{
    [Fact]
    public void FlightUserDateIndex_IsUnique_AndVehicleConcurrencyIsRetained()
    {
        using var context = new SkyShiftDbContext(new DbContextOptionsBuilder<SkyShiftDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);
        var flight = context.Model.FindEntityType(typeof(Flight))!;
        var index = Assert.Single(flight.GetIndexes(), i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserId", "Date" }));
        Assert.True(index.IsUnique);
        Assert.True(context.Model.FindEntityType(typeof(Vehicle))!.FindProperty("RowVersion")!.IsConcurrencyToken);
        Assert.True(context.Model.FindEntityType(typeof(User))!.FindProperty("Gender")!.IsNullable);
    }
    [Fact]
    public void Migration_MatchesCurrentModel_AndChecksDuplicatesBeforeSchemaChanges()
    {
        using var context = new SkyShiftDbContext(new DbContextOptionsBuilder<SkyShiftDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Trusted_Connection=True;TrustServerCertificate=True").Options);
        Assert.False(context.Database.HasPendingModelChanges());
        var script = context.GetService<IMigrator>().GenerateScript("20260910002608_AddManagerRoleAndTimestamps");
        var guard = script.IndexOf("THROW 51000", StringComparison.Ordinal);
        var schema = script.IndexOf("DROP INDEX", StringComparison.Ordinal);
        Assert.True(guard >= 0 && schema > guard);
        Assert.Contains("CREATE UNIQUE INDEX [IX_Flights_UserId_Date]", script);
        Assert.DoesNotContain("DELETE FROM", script);
    }
}
