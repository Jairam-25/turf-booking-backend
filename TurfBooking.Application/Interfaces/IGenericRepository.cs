using Domain.Specifications;

namespace Application.Interfaces;

public interface IGenericRepository<T>
    where T : class
{
    Task<IEnumerable<T>> FindAsync(Specification<T> spec, CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    IQueryable<T> AsQueryable();
}