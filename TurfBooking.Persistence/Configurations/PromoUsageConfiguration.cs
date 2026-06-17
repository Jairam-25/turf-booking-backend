using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class PromoUsageConfiguration : IEntityTypeConfiguration<PromoUsage>
    {
        public void Configure(EntityTypeBuilder<PromoUsage> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(p => p.PromoOffer)
                .WithMany(po => po.PromoUsages)
                .HasForeignKey(p => p.PromoOfferId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Booking)
                .WithMany()
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(p => new { p.UserId, p.PromoOfferId }).IsUnique();
        }
    }
}
