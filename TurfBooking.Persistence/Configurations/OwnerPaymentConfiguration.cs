using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class OwnerPaymentConfiguration : IEntityTypeConfiguration<OwnerPayment>
{
    public void Configure(EntityTypeBuilder<OwnerPayment> builder)
    {
        builder.HasKey(op => op.Id);

        builder.Property(op => op.Amount)
            .HasColumnType("decimal(18,2)");
    }
}
