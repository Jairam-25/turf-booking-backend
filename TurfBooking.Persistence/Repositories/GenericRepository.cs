using Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class GenericRepository<T>
    : IGenericRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
        => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public async Task Update(T entity)
        => _dbSet.Update(entity);

    public async Task Delete(T entity)
        => _dbSet.Remove(entity);

    public IQueryable<T> AsQueryable()
        => _dbSet.AsQueryable();
}