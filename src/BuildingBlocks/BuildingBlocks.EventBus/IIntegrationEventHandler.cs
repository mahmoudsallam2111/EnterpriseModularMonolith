namespace BuildingBlocks.EventBus;

/// <summary>
/// Implement this in a consuming module to react to an integration event.
/// Handlers run on the subscriber side and SHOULD be idempotent.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
