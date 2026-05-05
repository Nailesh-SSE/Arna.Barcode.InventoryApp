using InventoryManagement.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace InventoryManagement.Infrastructure.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[]? includes);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindWithIncludesAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    T Update(T entity);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    IQueryable<T> GetQueryable();
    IQueryable<T> GetQueryableWithIncludes(params Expression<Func<T, object>>[] includes);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly InventoryDbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(InventoryDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public IQueryable<T> GetQueryable()
    {
        try
        {
            return _dbSet.AsQueryable();
        }
        catch (ObjectDisposedException ex)
        {
            throw;
        }
    }

    public IQueryable<T> GetQueryableWithIncludes(params Expression<Func<T, object>>[] includes)
    {
        try
        {
            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return query;
        }
        catch (ObjectDisposedException ex)
        {
            throw;
        }
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[]? includes)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            IQueryable<T> query = _dbSet;
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _dbSet.ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities, cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public T Update(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            return entity;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null)
                return false;

            _dbSet.Remove(entity);
            return true;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _dbSet.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entity = await GetByIdAsync(id, cancellationToken);
            return entity != null;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public async Task<IEnumerable<T>> FindWithIncludesAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.Where(predicate).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
