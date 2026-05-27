using Application.Interfaces;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface ITurfRepository : IGenericRepository<Turf>
    {
        Task<Turf?> ValidateIdAsync(int? id, CancellationToken cancellationToken = default);
    }
}
