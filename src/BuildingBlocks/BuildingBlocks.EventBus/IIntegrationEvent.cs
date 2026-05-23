namespace BuildingBlocks.EventBus;

/// <summary>
/// Cross-module event published *after* the originating transaction commits.
/// Modules MUST communicate via integration events — never by calling another
/// module's domain types directly.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
    int Version { get; }
}

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    public virtual int Version => 1;
}
