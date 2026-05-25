namespace BuildingBlocks.Auditing.Entities;

/// <summary>
/// One row per audited operation (HTTP request or MediatR command/query).
/// Aggregate-like root of the audit model — owns its <see cref="EntityChanges"/>.
/// </summary>
public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Action { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public string? MethodName { get; set; }

    /// <summary>JSON-serialised arguments, with sensitive properties masked.</summary>
    public string? Parameters { get; set; }
    public string? ReturnValue { get; set; }

    public string? HttpMethod { get; set; }
    public string? Url { get; set; }
    public int? HttpStatusCode { get; set; }

    public string? ClientIp { get; set; }
    public string? ClientName { get; set; }
    public string? BrowserInfo { get; set; }
    public string? CorrelationId { get; set; }

    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? ImpersonatorUserId { get; set; }

    public DateTimeOffset ExecutionTime { get; set; } = DateTimeOffset.UtcNow;
    public int ExecutionDurationMs { get; set; }

    /// <summary>Full exception message + stack when the operation failed. Null on success.</summary>
    public string? Exception { get; set; }

    public List<EntityChange> EntityChanges { get; private set; } = [];
}
