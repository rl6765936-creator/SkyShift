using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SkyShift.Domain.Entities;

namespace SkyShift.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.AverageDailyTrips)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        // Persisted as string, not the enum's numeric value -- same
        // rationale as VehicleStatus: numeric storage would silently
        // corrupt existing rows if UserRole members are ever reordered.
        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SkyShift.Domain.Enums.UserRole.User);
    }
}
