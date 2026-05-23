namespace BuildingBlocks.EventBus;

/// <summary>
/// Application-layer queue for integration events. Command handlers call Enqueue;
/// the infrastructure interceptor persists queued events into the Outbox table inside
/// the same transaction as the aggregate change. Events become visible to consumers
/// only after commit, via the Outbox drain.
/// </summary>
public interface IIntegrationEventQueue
{
    void Enqueue(IIntegrationEvent integrationEvent, string? correlationId = null);
}
