using BuildingBlocks.Domain.Persistence;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence.Repositories;

/// <summary>
/// Aggregate-oriented repository. Tracking is enabled because changes will be
/// persisted via the ambient Unit of Work.
/// </summary>
public abstract class EfRepository<TDbContext, TAggregate, TId> : IRepository<TAggregate, TId>
    where TDbContext : DbContext
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    protected TDbContext Context { get; }
    protected DbSet<TAggregate> Set => Context.Set<TAggregate>();

    protected EfRepository(TDbContext context) => Context = context;

    public virtual Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(a => a.Id.Equals(id), cancellationToken);

    public virtual async Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(aggregate, cancellationToken);

    public virtual void Update(TAggregate aggregate) => Set.Update(aggregate);

    public virtual void Remove(TAggregate aggregate) => Set.Remove(aggregate);
}
