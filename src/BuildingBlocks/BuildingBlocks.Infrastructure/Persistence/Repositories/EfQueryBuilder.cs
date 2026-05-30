using System.Linq.Expressions;
using BuildingBlocks.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Repositories;

public class EfQueryBuilder<TDbContext, T> : IQueryBuilder<T> 
    where TDbContext : DbContext 
    where T : class
{
    private IQueryable<T> _query;

    public EfQueryBuilder(TDbContext dbContext)
    {
        _query = dbContext.Set<T>().AsNoTracking();
    }

    public IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        _query = _query.Where(predicate);
        return this;
    }

    public IQueryBuilder<T> Include(string navigationPropertyPath)
    {
        _query = _query.Include(navigationPropertyPath);
        return this;
    }

    public IQueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath)
    {
        _query = _query.Include(navigationPropertyPath);
        return this;
    }

    public IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        _query = _query.OrderBy(keySelector);
        return this;
    }

    public IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
    {
        _query = _query.OrderByDescending(keySelector);
        return this;
    }

    public IQueryBuilder<T> Skip(int count)
    {
        _query = _query.Skip(count);
        return this;
    }

    public IQueryBuilder<T> Take(int count)
    {
        _query = _query.Take(count);
        return this;
    }

    public Task<List<T>> ToListAsync(CancellationToken cancellationToken = default)
    {
        return _query.ToListAsync(cancellationToken);
    }

    public Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        return _query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return _query.AnyAsync(cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return _query.CountAsync(cancellationToken);
    }

    public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
    {
        return _query.Select(selector).ToListAsync(cancellationToken);
    }

    public Task<TResult?> SelectFirstOrDefaultAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default)
    {
        return _query.Select(selector).FirstOrDefaultAsync(cancellationToken);
    }

    public Task<long> LongCountAsync(CancellationToken cancellationToken = default)
    {
        return _query.LongCountAsync(cancellationToken);
    }
}
