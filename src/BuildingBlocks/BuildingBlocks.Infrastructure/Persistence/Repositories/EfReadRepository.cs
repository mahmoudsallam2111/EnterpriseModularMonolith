using System.Linq.Expressions;
using BuildingBlocks.Application.Persistence;
using BuildingBlocks.Domain;
using BuildingBlocks.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Repositories;

/// <summary>
/// Read repository. AsNoTracking by default, projection-friendly, supports specifications.
/// </summary>
public abstract class EfReadRepository<TDbContext, T> : IReadRepository<T>
    where TDbContext : DbContext
    where T : class
{
    protected TDbContext Context { get; }

    protected EfReadRepository(TDbContext context) => Context = context;

    protected IQueryable<T> Query(ISpecification<T> spec) =>
        SpecificationEvaluator.GetQuery(Context.Set<T>().AsQueryable(), spec);

    public Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        Query(spec).FirstOrDefaultAsync(cancellationToken);

    public Task<TProjection?> FirstOrDefaultAsync<TProjection>(
        ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector,
        CancellationToken cancellationToken = default) =>
        Query(spec).Select(selector).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        await Query(spec).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TProjection>> ListAsync<TProjection>(
        ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector,
        CancellationToken cancellationToken = default) =>
        await Query(spec).Select(selector).ToListAsync(cancellationToken);

    public async Task<PagedList<TProjection>> PageAsync<TProjection>(
        ISpecification<T> spec,
        Expression<Func<T, TProjection>> selector,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = Query(spec);
        var total = await baseQuery.LongCountAsync(cancellationToken);
        var items = await baseQuery.Skip(page.Skip).Take(page.Take)
            .Select(selector)
            .ToListAsync(cancellationToken);
        return new PagedList<TProjection>(items, page.Page, page.Take, total);
    }

    public Task<long> CountAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        Query(spec).LongCountAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken cancellationToken = default) =>
        Query(spec).AnyAsync(cancellationToken);
}
