namespace BuildingBlocks.EventBus.Outbox;

/// <summary>
/// Persistent envelope for an integration event waiting to be published.
/// Written in the SAME transaction as the aggregate change, then drained
/// by a background processor — this is the transactional outbox pattern.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Type { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTimeOffset OccurredOnUtc { get; private set; }
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }
    public string? CorrelationId { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string type, string payload, DateTimeOffset occurredOnUtc, string? correlationId)
    {
        Type = type;
        Payload = payload;
        OccurredOnUtc = occurredOnUtc;
        CorrelationId = correlationId;
    }

    public void MarkProcessed(DateTimeOffset whenUtc)
    {
        ProcessedOnUtc = whenUtc;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        Error = error;
    }
}
