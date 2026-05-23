namespace BuildingBlocks.EventBus;

/// <summary>
/// The integration event bus modules publish through. Implementation may be
/// in-process (default) or backed by a real broker like RabbitMQ/Azure Service Bus.
/// </summary>
public interface IIntegrationEventBus
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
