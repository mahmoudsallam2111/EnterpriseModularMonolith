using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.Auditing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace BuildingBlocks.Auditing.Jobs;

/// <summary>
/// Quartz job that deletes audit_logs older than <see cref="AuditingOptions.RetentionDays"/>.
/// Cascade FK deletes wipe associated entity_changes and entity_property_changes.
/// Runs in 10k chunks to avoid long-running transactions.
/// </summary>
[DisallowConcurrentExecution]
public sealed class AuditRetentionJob : IJob
{
    private const int ChunkSize = 10_000;
    private readonly AuditDbContext _db;
    private readonly AuditingOptions _options;
    private readonly ILogger<AuditRetentionJob> _logger;

    public AuditRetentionJob(AuditDbContext db, IOptions<AuditingOptions> options, ILogger<AuditRetentionJob> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_options.RetentionDays <= 0)
        {
            _logger.LogInformation("Audit retention is disabled (RetentionDays={Days}); skipping.", _options.RetentionDays);
            return;
        }

        var threshold = DateTimeOffset.UtcNow.AddDays(-_options.RetentionDays);
        _logger.LogInformation("Audit retention starting; deleting rows older than {Threshold}.", threshold);

        long totalDeleted = 0;
        while (!context.CancellationToken.IsCancellationRequested)
        {
            var deleted = await _db.AuditLogs
                .Where(l => l.ExecutionTime < threshold)
                .OrderBy(l => l.ExecutionTime)
                .Take(ChunkSize)
                .ExecuteDeleteAsync(context.CancellationToken);

            if (deleted == 0) break;
            totalDeleted += deleted;
            _logger.LogDebug("Audit retention deleted chunk of {Chunk}; total so far {Total}.", deleted, totalDeleted);
        }

        _logger.LogInformation("Audit retention finished; deleted {Total} rows.", totalDeleted);
    }
}
