using BuildingBlocks.Application.Auditing;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Auditing;

/// <summary>
/// Default audit logger — writes structured log entries that Serilog routes to Seq.
/// Replace with a database-backed writer if you need queryable audit trails.
/// </summary>
public sealed class LoggerAuditLogger : IAuditLogger
{
    private readonly ILogger<LoggerAuditLogger> _logger;

    public LoggerAuditLogger(ILogger<LoggerAuditLogger> logger) => _logger = logger;

    public Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AUDIT {Module} {Action} by {UserId} ({UserName}) on {EntityType}#{EntityId} at {OccurredOnUtc} meta={Metadata}",
            entry.Module, entry.Action, entry.UserId, entry.UserName,
            entry.EntityType, entry.EntityId, entry.OccurredOnUtc, entry.Metadata);
        return Task.CompletedTask;
    }
}
