namespace BuildingBlocks.Auditing.Abstractions;

/// <summary>
/// Submits a completed audit scope for persistence. Implementation is fire-and-forget;
/// the default <c>ChannelAuditWriter</c> enqueues onto a bounded channel that a background
/// service drains into the audit database.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Hand the scope to the writer. Must never throw on the caller's thread —
    /// audit failures must never break business operations.
    /// </summary>
    ValueTask WriteAsync(AuditScope scope, CancellationToken cancellationToken = default);
}
