namespace BuildingBlocks.EventBus.Outbox;

/// <summary>
/// Module-side outbox writer. Each module owns its own Outbox table inside its schema,
/// so integration event writes participate in the same transaction as the aggregate change.
/// </summary>
public interface IOutboxStore
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken);
}
