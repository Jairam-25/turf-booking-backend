using TurfBooking.Application.Interfaces;
using TurfBooking.Domain.Entities;
using TurfBooking.Persistence.Context;

namespace TurfBooking.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;


    public IGenericRepository<User> Users { get; }

    public IGenericRepository<Turf> Turfs { get; }

    public IGenericRepository<Slot> Slots { get; }

    public IGenericRepository<Booking> Bookings { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        Users = new GenericRepository<User>(_context);

        Turfs = new GenericRepository<Turf>(_context);

        Slots = new GenericRepository<Slot>(_context);

        Bookings = new GenericRepository<Booking>(_context);
    }

    public async Task<int> SaveAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}