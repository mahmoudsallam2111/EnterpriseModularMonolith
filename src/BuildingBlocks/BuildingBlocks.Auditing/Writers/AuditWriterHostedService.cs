using BuildingBlocks.Auditing.Abstractions;
using BuildingBlocks.Auditing.Entities;
using BuildingBlocks.Auditing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Auditing.Writers;

/// <summary>
/// Consumer side. Drains the channel in batches and inserts into AuditDbContext on
/// its own scope / its own transaction. Failures here are logged but never re-thrown —
/// audit writes degrade gracefully.
/// </summary>
public sealed class AuditWriterHostedService : BackgroundService
{
    private readonly ChannelAuditWriter _writer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditingOptions _options;
    private readonly ILogger<AuditWriterHostedService> _logger;

    public AuditWriterHostedService(
        ChannelAuditWriter writer,
        IServiceScopeFactory scopeFactory,
        IOptions<AuditingOptions> options,
        ILogger<AuditWriterHostedService> logger)
    {
        _writer = writer;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit writer started; capacity={Capacity}, batchSize={Batch}", _options.ChannelCapacity, _options.BatchSize);

        var batch = new List<AuditScope>(_options.BatchSize);
        var reader = _writer.Reader;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for at least one item.
                if (!await reader.WaitToReadAsync(stoppingToken)) break;

                batch.Clear();
                using var batchCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                batchCts.CancelAfter(_options.BatchTimeoutMs);

                while (batch.Count < _options.BatchSize && reader.TryRead(out var scope))
                    batch.Add(scope);

                while (batch.Count < _options.BatchSize)
                {
                    try
                    {
                        if (!await reader.WaitToReadAsync(batchCts.Token)) break;
                        while (batch.Count < _options.BatchSize && reader.TryRead(out var more))
                            batch.Add(more);
                    }
                    catch (OperationCanceledException) when (batchCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
                    {
                        // Batch timeout — flush what we have.
                        break;
                    }
                }

                if (batch.Count == 0) continue;
                await FlushAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit writer iteration failed");
                try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        // Final drain on shutdown — best effort, give it 5s.
        try
        {
            using var graceCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            batch.Clear();
            while (reader.TryRead(out var scope))
                batch.Add(scope);
            if (batch.Count > 0)
                await FlushAsync(batch, graceCts.Token);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Audit writer final drain failed."); }

        _logger.LogInformation("Audit writer stopped.");
    }

    private async Task FlushAsync(List<AuditScope> batch, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();

        foreach (var s in batch)
        {
            var log = new AuditLog
            {
                Action = s.Action,
                ServiceName = s.ServiceName,
                MethodName = s.MethodName,
                Parameters = s.Parameters,
                ReturnValue = s.ReturnValue,
                HttpMethod = s.HttpMethod,
                Url = s.Url,
                HttpStatusCode = s.HttpStatusCode,
                ClientIp = s.ClientIp,
                ClientName = s.ClientName,
                BrowserInfo = s.BrowserInfo,
                CorrelationId = s.CorrelationId,
                UserId = s.UserId,
                UserName = s.UserName,
                TenantId = s.TenantId,
                ImpersonatorUserId = s.ImpersonatorUserId,
                ExecutionTime = s.StartedAtUtc,
                ExecutionDurationMs = s.DurationMs,
                Exception = s.Exception
            };

            foreach (var ec in s.EntityChanges)
            {
                var change = new EntityChange
                {
                    AuditLogId = log.Id,
                    ChangeTime = ec.ChangeTime,
                    ChangeType = (EntityChangeType)ec.ChangeType,
                    EntityId = ec.EntityId,
                    EntityType = ec.EntityType,
                    EntityTenantId = ec.EntityTenantId,
                    Module = ec.Module
                };

                foreach (var pc in ec.Properties)
                {
                    change.PropertyChanges.Add(new EntityPropertyChange
                    {
                        EntityChangeId = change.Id,
                        PropertyName = pc.PropertyName,
                        PropertyType = pc.PropertyType,
                        OriginalValue = pc.OriginalValue,
                        NewValue = pc.NewValue
                    });
                }

                log.EntityChanges.Add(change);
            }

            db.AuditLogs.Add(log);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audit batch flush failed (size={Size}); dropping.", batch.Count);
        }
    }
}
