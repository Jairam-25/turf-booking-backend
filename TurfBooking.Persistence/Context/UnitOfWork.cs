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

        public IReviewRepository Reviews { get; }

        public UnitOfWork
        (
            ApplicationDbContext context,
            IUserRepository users,
            IBookingRepository bookings,
            ITurfRepository turfs,
            ISlotRepository slots,
            IReviewRepository reviews
        )
        {
            _context = context;
            Users = users;
            Bookings = bookings;
            Turfs = turfs;
            Slots = slots;
            Reviews = reviews;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}