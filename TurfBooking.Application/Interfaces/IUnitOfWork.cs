using Domain.Entities;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Booking> Bookings { get; }

    IGenericRepository<Turf> Turfs { get; }

    Task<int> SaveChangesAsync();
}