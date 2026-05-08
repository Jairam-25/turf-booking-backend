using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.Domain.Entities;

namespace TurfBooking.Persistence.Configurations;

public class SlotConfiguration : IEntityTypeConfiguration<Slot>
{
    public void Configure(EntityTypeBuilder<Slot> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartTime)
            .IsRequired();

        builder.Property(s => s.EndTime)
            .IsRequired();

        builder.Property(s => s.IsBooked)
            .HasDefaultValue(false);

        // One Slot -> Many Bookings
        builder.HasMany(s => s.Bookings)
            .WithOne(b => b.Slot)
            .HasForeignKey(b => b.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}