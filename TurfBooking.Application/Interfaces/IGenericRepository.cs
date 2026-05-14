namespace Application.Interfaces;

public interface IGenericRepository<T>
    where T : class
{
    Task<IEnumerable<T>> GetAllAsync();

    Task<T?> GetByIdAsync(Guid id);

    Task AddAsync(T entity);

    Task Update(T entity);

    Task Delete(T entity);

    IQueryable<T> AsQueryable();
}