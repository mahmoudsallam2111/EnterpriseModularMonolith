using System.Collections.ObjectModel;

namespace BuildingBlocks.Auditing.Abstractions;

/// <summary>
/// Bound from the <c>"Auditing"</c> configuration section. All knobs live here so
/// consumers tune behaviour without changing code.
/// </summary>
public sealed class AuditingOptions
{
    public const string SectionName = "Auditing";

    /// <summary>Master switch. When false, no audit rows are produced.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Audit queries (IQuery&lt;T&gt;), not just commands. Default: false.</summary>
    public bool AuditQueries { get; set; }

    /// <summary>Also write rows for handlers that threw. Default: true.</summary>
    public bool AuditFailures { get; set; } = true;

    /// <summary>In-memory channel capacity. When full, oldest items are dropped with a warning.</summary>
    public int ChannelCapacity { get; set; } = 10_000;

    /// <summary>Drain batch size — writer flushes when this many scopes are queued.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>Or when this much time has passed since the first scope in the current batch.</summary>
    public int BatchTimeoutMs { get; set; } = 200;

    /// <summary>Hard cap on persisted property value length (chars). Long values are clipped.</summary>
    public int MaxPropertyValueLength { get; set; } = 1000;

    /// <summary>Label written when no user is authenticated.</summary>
    public string AnonymousUserName { get; set; } = "anonymous";

    /// <summary>Retention in days for the periodic janitor. 0 = never delete.</summary>
    public int RetentionDays { get; set; } = 365;

    /// <summary>Cron expression for the janitor job (default: 03:15 daily).</summary>
    public string RetentionCron { get; set; } = "0 15 3 * * ?";

    /// <summary>Property names matched case-insensitively that should be masked in parameter / value capture.</summary>
    public Collection<string> SensitivePropertyNames { get; } =
    [
        "password", "passwordhash", "token", "secret",
        "apikey", "signingkey", "authorization", "creditcard"
    ];

    /// <summary>Entity full type names to never audit even if they implement <see cref="IAuditedEntity"/>.</summary>
    public Collection<string> ExcludedEntityTypes { get; } =
    [
        "BuildingBlocks.EventBus.Outbox.OutboxMessage",
        "BuildingBlocks.EventBus.Inbox.InboxMessage"
    ];
}
