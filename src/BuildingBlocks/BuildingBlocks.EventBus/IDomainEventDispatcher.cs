using BuildingBlocks.Domain;

namespace BuildingBlocks.EventBus;

/// <summary>
/// Dispatches domain events inside the current transaction, before commit.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);
}
