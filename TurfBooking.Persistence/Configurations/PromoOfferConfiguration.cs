using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations
{
    public class PromoOfferConfiguration : IEntityTypeConfiguration<PromoOffer>
    {
        public void Configure(EntityTypeBuilder<PromoOffer> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.PromoCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.HasIndex(p => p.PromoCode).IsUnique();

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.DiscountPercentage)
                .HasColumnType("decimal(5,2)");
        }
    }
}
