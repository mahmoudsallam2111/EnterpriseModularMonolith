namespace BuildingBlocks.Application.Auditing;

/// <summary>
/// Records user-facing audit entries (who did what, when, on which entity).
/// Not the same as EF Core change-tracking — this is business-level audit.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

public sealed record AuditEntry(
    string Action,
    string Module,
    string? EntityType,
    string? EntityId,
    Guid? UserId,
    string? UserName,
    DateTimeOffset OccurredOnUtc,
    IReadOnlyDictionary<string, string>? Metadata = null);
