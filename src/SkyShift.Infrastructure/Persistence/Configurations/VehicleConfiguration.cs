using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedOnAdd();

        // Persisted as string, not the enum's numeric value: VehicleStatus
        // has already lost a member once (Reserved), and numeric storage
        // would silently corrupt existing rows if members are ever
        // reordered or removed in the future.
        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(v => v.IsPersonalExperimental)
            .IsRequired();

        // Server-only, UTC. NOT NULL with a SQL-side default so the
        // migration can backfill any pre-existing rows at the moment the
        // column is added (SQL Server evaluates the DEFAULT for existing
        // rows when a NOT NULL column is introduced) -- no fabricated
        // "real" creation date is claimed for those rows, just the safe,
        // documented migration-apply time.
        builder.Property(v => v.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Shadow property: optimistic-concurrency token used to detect two
        // concurrent SelectVehicle attempts against the same Vehicle
        // (Problem 1). Kept out of the Domain entity -- RowVersion is a
        // persistence concern, not a business concept.
        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();
    }
}
