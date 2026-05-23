using System.Linq.Expressions;
using BuildingBlocks.Domain;
using BuildingBlocks.SharedKernel;

namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// Read-side repository. No tracking, projection-friendly, supports specifications and paging.
/// Use for queries — never to load an aggregate that will be modified.
/// </summary>
public interface IReadRepository<T>
    where T : class
{
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<TProjection?> FirstOrDefaultAsync<TProjection>(
        ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TProjection>> ListAsync<TProjection>(
        ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector,
        CancellationToken cancellationToken = default);

    Task<PagedList<TProjection>> PageAsync<TProjection>(
        ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector,
        PageRequest page,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
}
