using BuildingBlocks.Domain;
using BuildingBlocks.EventBus;

namespace EnterpriseModularMonolith.Migrator;

internal sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
{
    public Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
