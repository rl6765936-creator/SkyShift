using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence.Configurations;

public class FlightConfiguration : IEntityTypeConfiguration<Flight>
{
    public void Configure(EntityTypeBuilder<Flight> builder)
    {
        builder.ToTable("Flights");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .ValueGeneratedOnAdd();

        builder.Property(f => f.UserId)
            .IsRequired();

        // Nullable: Domain allows a Flight to exist without a Vehicle.
        builder.Property(f => f.VehicleId);

        builder.Property(f => f.Date)
            .IsRequired();

        // Server-only, UTC. Distinct from Date -- see Flight.CreatedAt's
        // doc comment. Used for Manager activity trends/audit, never for
        // the existing Compliance calculation (which stays on Date). NOT
        // NULL with a SQL-side default so the migration can backfill any
        // pre-existing rows -- see VehicleConfiguration.CreatedAt for the
        // same rationale.
        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vehicle>()
            .WithMany()
            .HasForeignKey(f => f.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supports GetCompletedCountAsync's WHERE UserId = @userId AND Date
        // BETWEEN @start AND @end.
        builder.HasIndex(f => new { f.UserId, f.Date }).IsUnique();
    }
}
