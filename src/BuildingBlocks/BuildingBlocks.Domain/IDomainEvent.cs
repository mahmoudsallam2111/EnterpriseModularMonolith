using MediatR;

namespace BuildingBlocks.Domain;

/// <summary>
/// Marker for domain events. Domain events are raised inside aggregates and are
/// dispatched in-process, inside the same transaction as the change that produced them.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOnUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
