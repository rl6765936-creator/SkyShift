using Microsoft.EntityFrameworkCore;
using SkyShift.Domain.Entities;
using SkyShift.Infrastructure;
using SkyShift.Infrastructure.Persistence;
using Xunit;

namespace SkyShift.Tests.PostgreSql;

/// <summary>
/// Provider selection and model checks that need no database server: each provider's model matches
/// its own migrations (so the SQL Server model is unchanged by the PostgreSQL additions), and the
/// configured provider name is parsed explicitly.
/// </summary>
public class ProviderModelTests
{
    private static SkyShiftDbContext Context(DatabaseProvider provider, string connectionString)
    {
        var builder = new DbContextOptionsBuilder<SkyShiftDbContext>();
        DependencyInjection.UseProvider(builder, connectionString, provider);
        return new SkyShiftDbContext(builder.Options);
    }

    [Fact]
    public void SqlServerModel_StillMatchesTheExistingSqlServerMigrations()
    {
        using var context = Context(DatabaseProvider.SqlServer, "Server=model-check-only;Database=none");
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Contains("20261011090000_CompleteCatalogSpecifications", context.Database.GetMigrations());
        Assert.DoesNotContain(context.Database.GetMigrations(), m => m.EndsWith("_InitialPostgreSql", StringComparison.Ordinal));

        var vehicle = context.Model.FindEntityType(typeof(Vehicle))!;
        Assert.NotNull(vehicle.FindProperty("RowVersion"));
        Assert.Null(vehicle.FindProperty("xmin"));
    }

    [Fact]
    public void PostgreSqlModel_MatchesItsOwnSeparateMigrations()
    {
        using var context = Context(DatabaseProvider.PostgreSql, "Host=model-check-only;Database=none");
        Assert.False(context.Database.HasPendingModelChanges());
        Assert.Equal(new[] { "InitialPostgreSql" }, context.Database.GetMigrations().Select(m => m[15..]));

        var vehicle = context.Model.FindEntityType(typeof(Vehicle))!;
        Assert.True(vehicle.FindProperty("xmin")!.IsConcurrencyToken);
        Assert.Null(vehicle.FindProperty("RowVersion"));
        Assert.Equal("\"CatalogItemId\" IS NOT NULL", vehicle.GetIndexes().Single(i => i.Properties.Any(p => p.Name == "CatalogItemId")).GetFilter());
    }

    [Theory]
    [InlineData(null, DatabaseProvider.SqlServer)]
    [InlineData("", DatabaseProvider.SqlServer)]
    [InlineData("SqlServer", DatabaseProvider.SqlServer)]
    [InlineData("PostgreSQL", DatabaseProvider.PostgreSql)]
    [InlineData("postgresql", DatabaseProvider.PostgreSql)]
    public void ProviderName_IsParsedExplicitly(string? value, DatabaseProvider expected) =>
        Assert.Equal(expected, DatabaseProviderNames.Parse(value));

    [Theory]
    [InlineData("Postgres")]
    [InlineData("Sqlite")]
    public void UnknownProviderName_FailsFast(string value) =>
        Assert.Throws<InvalidOperationException>(() => DatabaseProviderNames.Parse(value));
}
