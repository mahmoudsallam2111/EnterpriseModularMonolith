namespace BuildingBlocks.EventBus.Inbox;

/// <summary>
/// Records that an integration event has been processed by this module, enabling
/// idempotent consumption — a duplicate delivery is detected by EventId and ignored.
/// </summary>
public sealed class InboxMessage
{
    public Guid EventId { get; private set; }
    public string Type { get; private set; } = default!;
    public DateTimeOffset ReceivedOnUtc { get; private set; }
    public DateTimeOffset? ProcessedOnUtc { get; private set; }
    public string? Error { get; private set; }
    public int Attempts { get; private set; }

    private InboxMessage() { }

    public InboxMessage(Guid eventId, string type, DateTimeOffset receivedOnUtc)
    {
        EventId = eventId;
        Type = type;
        ReceivedOnUtc = receivedOnUtc;
    }

    public void MarkProcessed(DateTimeOffset whenUtc) => ProcessedOnUtc = whenUtc;
    public void MarkFailed(string error) { Attempts++; Error = error; }
}

public interface IInboxStore
{
    Task<bool> AlreadyProcessedAsync(Guid eventId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(Guid eventId, string type, CancellationToken cancellationToken);
}
