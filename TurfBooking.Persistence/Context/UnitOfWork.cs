using Application.Interfaces;
using Domain.Entities;
using Persistence.Context;

namespace Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IGenericRepository<User> Users { get; }

    public IGenericRepository<Booking> Bookings { get; }

    public IGenericRepository<Turf> Turfs { get; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        Users = new GenericRepository<User>(_context);

        Bookings = new GenericRepository<Booking>(_context);

        Turfs = new GenericRepository<Turf>(_context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}