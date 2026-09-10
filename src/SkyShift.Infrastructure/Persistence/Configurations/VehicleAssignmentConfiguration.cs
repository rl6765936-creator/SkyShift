using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence.Configurations;

public class VehicleAssignmentConfiguration : IEntityTypeConfiguration<VehicleAssignment>
{
    public void Configure(EntityTypeBuilder<VehicleAssignment> builder)
    {
        builder.ToTable("VehicleAssignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.VehicleId)
            .IsRequired();

        builder.Property(a => a.StartDate)
            .IsRequired();

        builder.Property(a => a.EndDate)
            .IsRequired();

        // Many-to-many relationship: a User can hold many VehicleAssignments
        // over time (each to a different Vehicle), and a Vehicle can
        // likewise be assigned to many different Users over time -- realized
        // through this entity as the associative/join type, rather than a
        // bare skip-navigation join table, because each pairing also carries
        // its own StartDate/EndDate. Same two FK columns/constraints as
        // before (HasForeignKey, OnDelete) -- only the navigation properties
        // are new, so this does not change the database schema.
        builder.HasOne(a => a.User)
            .WithMany(u => u.VehicleAssignments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Vehicle)
            .WithMany(v => v.VehicleAssignments)
            .HasForeignKey(a => a.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        // User.VehicleAssignments/Vehicle.VehicleAssignments are exposed as
        // a read-only IReadOnlyCollection<VehicleAssignment> with no public
        // setter, backed by a private List<VehicleAssignment> field named
        // "_vehicleAssignments" -- EF Core's default convention detects that
        // backing field automatically from the property name (the standard
        // "_camelCase" pattern) and materializes through it directly, so no
        // explicit UsePropertyAccessMode call is required here.

        // Supports GetActiveForUserAsync's WHERE UserId = @userId filter.
        // Not, by itself, a concurrency guarantee -- see
        // SkyShiftDbContext/UnitOfWork for the Serializable-transaction
        // protection against two concurrent SelectVehicle calls for the
        // same user.
        builder.HasIndex(a => a.UserId);
    }
}
