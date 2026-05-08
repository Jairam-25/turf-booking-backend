using TurfBooking.Domain.Entities;

namespace TurfBooking.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<User> Users { get; }

    IGenericRepository<Turf> Turfs { get; }

    IGenericRepository<Slot> Slots { get; }

    IGenericRepository<Booking> Bookings { get; }

    Task<int> SaveAsync();
}