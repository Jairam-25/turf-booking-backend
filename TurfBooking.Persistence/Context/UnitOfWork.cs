using Application.Interfaces;
using Domain.Entities;
using Application.Interfaces;
using Persistence.Repositories;

namespace Persistence.Context
{

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public IUserRepository Users { get; }

        public IBookingRepository Bookings { get; }

        public ITurfRepository Turfs { get; }

        public ISlotRepository Slots { get; }

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            Users = new UserRepository(_context);

            Bookings = new BookingRepository(_context);

            Turfs = new TurfRepository(_context);

            Slots = new SlotRepository(_context);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}