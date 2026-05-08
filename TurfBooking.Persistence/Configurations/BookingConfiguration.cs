using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TurfBooking.Domain.Entities;

namespace TurfBooking.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingDate)
            .IsRequired();

        // User Relationship
        builder.HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Slot Relationship
        builder.HasOne(b => b.Slot)
            .WithMany(s => s.Bookings)
            .HasForeignKey(b => b.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}