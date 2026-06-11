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

        public IGenericRepository<OwnerRequest> OwnerRequests { get; }
        public IGenericRepository<AuditLog> AuditLogs { get; }
        public IGenericRepository<Owner> Owners { get; }
        public IGenericRepository<OwnerPayment> OwnerPayments { get; }
        public IGenericRepository<TurfDocument> TurfDocuments { get; }
        public IGenericRepository<TurfImage> TurfImages { get; }

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
            OwnerRequests = new GenericRepository<OwnerRequest>(_context);
            AuditLogs = new GenericRepository<AuditLog>(_context);
            Owners = new GenericRepository<Owner>(_context);
            OwnerPayments = new GenericRepository<OwnerPayment>(_context);
            TurfDocuments = new GenericRepository<TurfDocument>(_context);
            TurfImages = new GenericRepository<TurfImage>(_context);
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}