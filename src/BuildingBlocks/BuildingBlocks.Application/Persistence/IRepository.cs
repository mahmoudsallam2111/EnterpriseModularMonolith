using BuildingBlocks.Domain;

namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// Repository pattern for aggregates. Tracking is enabled by default.
/// One repository per aggregate root.
/// </summary>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    void Update(TAggregate aggregate);
    void Remove(TAggregate aggregate);
}
