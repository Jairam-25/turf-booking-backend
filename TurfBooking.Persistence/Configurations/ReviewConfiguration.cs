using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.Rating)
            .IsRequired();

        // One Turf -> Many Reviews
        builder.HasOne(r => r.Turf)
            .WithMany(t => t.Reviews)
            .HasForeignKey(r => r.TurfId)
            .OnDelete(DeleteBehavior.Cascade);

        // One User -> Many Reviews (A user can have many reviews, but we delete reviews when user is deleted)
        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
