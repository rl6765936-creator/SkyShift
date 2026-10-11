using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL-only adjustments applied after the shared entity configurations, so the SQL Server
/// model (and its migrations) stays exactly as it was. Each change keeps the SQL Server behaviour:
/// UTC timestamps, a case-insensitive unique email, the filtered catalog index and an optimistic
/// concurrency token on Vehicle.
/// </summary>
internal static class PostgreSqlModel
{
    // Case-insensitive, accent-sensitive, like SQL Server's default *_CI_AS collation, so the
    // unique email index and email lookups treat "A@x" and "a@x" as the same address.
    internal const string CaseInsensitiveCollation = "skyshift_case_insensitive";

    // timestamptz only accepts UTC values. The application already uses UTC everywhere
    // (SQL Server stores it without a time zone), so Unspecified is read as UTC and Local
    // (for example a bound query-string value) is converted. Reads come back as UTC.
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => value.Kind == DateTimeKind.Utc ? value
            : value.Kind == DateTimeKind.Local ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    internal static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCollation(CaseInsensitiveCollation, locale: "und-u-ks-level2", provider: "icu", deterministic: false);
        modelBuilder.Entity<User>().Property(u => u.Email).UseCollation(CaseInsensitiveCollation);

        modelBuilder.Entity<Flight>().Property(f => f.CreatedAt).HasDefaultValueSql("now()");
        modelBuilder.Entity<Vehicle>().Property(v => v.CreatedAt).HasDefaultValueSql("now()");

        modelBuilder.Entity<Vehicle>().HasIndex(v => v.CatalogItemId)
            .IsUnique()
            .HasFilter("\"CatalogItemId\" IS NOT NULL");

        // SQL Server's rowversion becomes PostgreSQL's built-in xmin system column, which changes
        // on every row update; EF Core checks it in the UPDATE/DELETE WHERE clause.
        modelBuilder.Entity<Vehicle>().Property<uint>("xmin").IsRowVersion();

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entity => entity.GetProperties())
                     .Where(property => property.ClrType == typeof(DateTime)))
        {
            property.SetValueConverter(UtcConverter);
        }
    }
}
