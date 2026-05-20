using Application.Interfaces;
using Domain.Entities;
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

        public UnitOfWork
        (
            ApplicationDbContext context,
            IUserRepository users,
            IBookingRepository bookings,
            ITurfRepository turfs,
            ISlotRepository slots
        )
        {
            _context = context;
            Users = users;
            Bookings = bookings;
            Turfs = turfs;
            Slots = slots;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}