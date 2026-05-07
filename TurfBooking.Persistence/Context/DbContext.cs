using Microsoft.EntityFrameworkCore;
using TurfBooking.Domain.Entities;

namespace TurfBooking.Persistence.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
}