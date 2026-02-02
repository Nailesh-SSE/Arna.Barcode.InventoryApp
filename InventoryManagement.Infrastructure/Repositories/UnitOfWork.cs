
using InventoryManagement.Core.Data;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Infrastructure.Repositories;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> GetRepository<T>() where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly InventoryDbContext _context;
    private readonly Dictionary<Type, object> _repositories;
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(InventoryDbContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
        _repositories = new Dictionary<Type, object>();
    }

    public IRepository<T> GetRepository<T>() where T : class
    {
        try
        {
            if (_repositories.ContainsKey(typeof(T)))
            {
                return (IRepository<T>)_repositories[typeof(T)];
            }
            var repository = new Repository<T>(_context);
            _repositories.Add(typeof(T), repository);
            return repository;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "DbContext is disposed when getting repository for {EntityType}", typeof(T).Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository for {EntityType}", typeof(T).Name);
            throw;
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("SaveChangesAsync operation was cancelled");
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "DbContext is disposed in SaveChangesAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SaveChangesAsync");
            throw;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("BeginTransactionAsync operation was cancelled");
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "DbContext is disposed in BeginTransactionAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in BeginTransactionAsync");
            throw;
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("CommitTransactionAsync operation was cancelled");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "DbContext is disposed in CommitTransactionAsync");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CommitTransactionAsync");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("RollbackTransactionAsync operation was cancelled");
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "DbContext is disposed in RollbackTransactionAsync");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RollbackTransactionAsync");
        }
    }

    public void Dispose()
    {
        try
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing UnitOfWork");
        }
    }
}
