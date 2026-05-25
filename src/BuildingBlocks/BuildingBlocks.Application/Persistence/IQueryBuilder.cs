using System.Linq.Expressions;

namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// A query builder abstraction. The default behavior is No-Tracking.
/// Used for querying the database efficiently for read operations.
/// </summary>
public interface IQueryBuilder<T> where T : class
{
    IQueryBuilder<T> Where(Expression<Func<T, bool>> predicate);
    IQueryBuilder<T> Include(string navigationPropertyPath);
    IQueryBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> navigationPropertyPath);
    IQueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector);
    IQueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector);
    IQueryBuilder<T> Skip(int count);
    IQueryBuilder<T> Take(int count);

    Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<List<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector, CancellationToken cancellationToken = default);
}
