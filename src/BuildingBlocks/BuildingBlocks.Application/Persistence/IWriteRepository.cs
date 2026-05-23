using BuildingBlocks.Domain;

namespace BuildingBlocks.Application.Persistence;

/// <summary>
/// Write-side repository. Aggregate-oriented, tracked, used inside a Unit of Work.
/// One repository per aggregate root — never one giant generic repository.
/// </summary>
public interface IWriteRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    void Update(TAggregate aggregate);
    void Remove(TAggregate aggregate);
}
