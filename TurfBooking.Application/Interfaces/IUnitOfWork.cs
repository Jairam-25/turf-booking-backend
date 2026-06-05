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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}