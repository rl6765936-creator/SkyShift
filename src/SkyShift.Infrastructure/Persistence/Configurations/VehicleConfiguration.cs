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

        builder.Property(v => v.ModelName).HasMaxLength(200);
        builder.Property(v => v.MaxSpeedKmh);
        builder.Property(v => v.Seats);
        builder.Property(v => v.ImageUrl).HasMaxLength(2048);

        // Catalog link and the catalog metadata copied at creation time.
        // All nullable: legacy vehicles stay unlinked with unknown values.
        builder.Property(v => v.Manufacturer).HasMaxLength(100);
        builder.Property(v => v.Description).HasMaxLength(500);
        builder.Property(v => v.DescriptionHe).HasMaxLength(500);
        builder.Property(v => v.CatalogIdentification)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(v => v.SeatsAccuracy)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(v => v.MaxRoadSpeedAccuracy)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne<VehicleCatalogItem>()
            .WithMany()
            .HasForeignKey(v => v.CatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // A catalog model can back at most one fleet vehicle, ever -- also
        // when that vehicle is later retired. Filtered so any number of
        // legacy (NULL) vehicles remain valid. This is the final guard for
        // two Managers adding the same model concurrently; UnitOfWork
        // translates its violation into CatalogItemInUseException.
        builder.HasIndex(v => v.CatalogItemId)
            .IsUnique()
            .HasFilter("[CatalogItemId] IS NOT NULL");

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
