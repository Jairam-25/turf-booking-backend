using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.Domain.Entities;

namespace TurfBooking.Persistence.Configurations;

public class TurfConfiguration : IEntityTypeConfiguration<Turf>
{
    public void Configure(EntityTypeBuilder<Turf> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.PricePerHour)
            .HasColumnType("decimal(18,2)");

        // One Turf -> Many Slots
        builder.HasMany(t => t.Slots)
            .WithOne(s => s.Turf)
            .HasForeignKey(s => s.TurfId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}