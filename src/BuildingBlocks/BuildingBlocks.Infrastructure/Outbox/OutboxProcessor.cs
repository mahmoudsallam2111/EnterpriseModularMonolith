using System.Text.Json;
using BuildingBlocks.EventBus;
using BuildingBlocks.EventBus.Outbox;
using BuildingBlocks.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Background drain for a single module's Outbox table. Reads unpublished messages,
/// deserialises them, calls the integration event bus, and marks them processed.
/// Idempotent at the consumer side via the Inbox table.
/// </summary>
public sealed class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor<TDbContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor for {DbContext} started", typeof(TDbContext).Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessOnceAsync(stoppingToken); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing iteration failed");
            }
            try { await Task.Delay(_pollInterval, stoppingToken); }
            catch (TaskCanceledException) { /* shutdown */ }
        }
    }

    private async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IIntegrationEventBus>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var set = db.Set<OutboxMessage>();
        var pending = await set
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0) return;

        foreach (var message in pending)
        {
            try
            {
                var type = Type.GetType(message.Type)
                    ?? throw new InvalidOperationException($"Cannot resolve event type {message.Type}");
                var evt = (IIntegrationEvent)JsonSerializer.Deserialize(message.Payload, type)!;
                await PublishViaReflectionAsync(bus, evt, type, cancellationToken);
                message.MarkProcessed(clock.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {Id}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Task PublishViaReflectionAsync(IIntegrationEventBus bus, IIntegrationEvent evt, Type type, CancellationToken cancellationToken)
    {
        var method = typeof(IIntegrationEventBus)
            .GetMethod(nameof(IIntegrationEventBus.PublishAsync))!
            .MakeGenericMethod(type);
        return (Task)method.Invoke(bus, [evt, cancellationToken])!;
    }
}
