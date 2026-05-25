namespace BuildingBlocks.Auditing.Abstractions;

/// <summary>
/// The in-flight audit unit. One scope = one row in <c>audit_logs</c>.
/// The middleware opens an outer scope per HTTP request; the MediatR behavior
/// opens a nested scope per command/query. The interceptor attaches captured
/// entity changes to <see cref="EntityChanges"/> on whichever scope is current
/// at SaveChanges time.
/// </summary>
public sealed class AuditScope
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Action { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }
    public string? Parameters { get; set; }
    public string? ReturnValue { get; set; }
    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ClientIp { get; set; }
    public string? ClientName { get; set; }
    public string? BrowserInfo { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ImpersonatorUserId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Exception { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int DurationMs { get; set; }

    public List<PendingEntityChange> EntityChanges { get; } = [];
}

/// <summary>
/// Mutable carrier passed from the interceptor to the writer.
/// </summary>
public sealed class PendingEntityChange
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? EntityTenantId { get; set; }
    public string? Module { get; set; }
    public int ChangeType { get; set; }
    public DateTimeOffset ChangeTime { get; set; }
    public List<PendingPropertyChange> Properties { get; } = [];
}

public sealed class PendingPropertyChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? NewValue { get; set; }
}
