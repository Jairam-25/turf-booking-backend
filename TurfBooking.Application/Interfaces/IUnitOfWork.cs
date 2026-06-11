using Domain.Entities;

namespace Application.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }

    IBookingRepository Bookings { get; }

    ITurfRepository Turfs { get; }

    ISlotRepository Slots { get; }

    IReviewRepository Reviews { get; }

    IGenericRepository<OwnerRequest> OwnerRequests { get; }

    IGenericRepository<AuditLog> AuditLogs { get; }

    IGenericRepository<Owner> Owners { get; }

    IGenericRepository<OwnerPayment> OwnerPayments { get; }

    IGenericRepository<TurfDocument> TurfDocuments { get; }

    IGenericRepository<TurfImage> TurfImages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}