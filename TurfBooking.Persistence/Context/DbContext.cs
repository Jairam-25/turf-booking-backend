using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Configurations;

namespace Persistence.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Turf> Turfs { get; set; }
    public DbSet<Slot> Slots { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        // Apply all entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TurfConfiguration());
        modelBuilder.ApplyConfiguration(new SlotConfiguration());
        modelBuilder.ApplyConfiguration(new BookingConfiguration());

        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        modelBuilder.Entity<Turf>()
            .HasQueryFilter(t => !t.IsDeleted);

        modelBuilder.Entity<Slot>()
            .HasQueryFilter(s => !s.IsDeleted);

        modelBuilder.Entity<Booking>()
            .HasQueryFilter(b => !b.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }
}