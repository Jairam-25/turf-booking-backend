using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

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
            .HasMaxLength(1000);

        builder.Property(t => t.PricePerHour)
            .HasColumnType("decimal(18,2)");

        // One Turf -> Many Slots
        builder.HasMany(t => t.Slots)
            .WithOne(s => s.Turf)
            .HasForeignKey(s => s.TurfId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Turf -> Many Reviews
        builder.HasMany(t => t.Reviews)
            .WithOne(r => r.Turf)
            .HasForeignKey(r => r.TurfId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Turf -> One Owner
        builder.HasOne(t => t.Owner)
            .WithMany(o => o.Turfs)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        // One Turf -> Many Documents
        builder.HasMany(t => t.Documents)
            .WithOne(d => d.Turf)
            .HasForeignKey(d => d.TurfId)
            .OnDelete(DeleteBehavior.Cascade);

        // One Turf -> Many Images
        builder.HasMany(t => t.Images)
            .WithOne(i => i.Turf)
            .HasForeignKey(i => i.TurfId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}