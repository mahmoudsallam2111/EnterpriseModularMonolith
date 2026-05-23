using BuildingBlocks.Domain;
using MediatR;

namespace BuildingBlocks.EventBus.InProcess;

/// <summary>
/// Default in-process domain event dispatcher backed by MediatR.Publish.
/// Domain events are dispatched inside the active transaction, before commit.
/// </summary>
public sealed class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IPublisher _publisher;

    public MediatRDomainEventDispatcher(IPublisher publisher) => _publisher = publisher;

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, cancellationToken);
    }
}
