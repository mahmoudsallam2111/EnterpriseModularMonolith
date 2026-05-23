using System.Text.Json;
using BuildingBlocks.EventBus;
using BuildingBlocks.EventBus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Captures integration events queued during the unit of work and persists them
/// to the module's Outbox table in the SAME transaction as the aggregate change.
///
/// Modules call <see cref="OutboxAccumulator.Enqueue"/> from their command handlers
/// (via the <see cref="IIntegrationEventQueue"/> abstraction) instead of publishing
/// directly — that's how at-least-once delivery is guaranteed without distributed
/// transactions.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly OutboxAccumulator _accumulator;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OutboxInterceptor(OutboxAccumulator accumulator) => _accumulator = accumulator;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Persist(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Persist(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Persist(DbContext context)
    {
        var pending = _accumulator.Drain();
        if (pending.Count == 0) return;

        var set = context.Set<OutboxMessage>();
        foreach (var (evt, correlationId) in pending)
        {
            var payload = JsonSerializer.Serialize(evt, evt.GetType(), JsonOptions);
            set.Add(new OutboxMessage(
                type: evt.GetType().AssemblyQualifiedName!,
                payload: payload,
                occurredOnUtc: evt.OccurredOnUtc,
                correlationId: correlationId));
        }
    }
}

/// <summary>
/// Scoped accumulator. Command handlers push integration events here through
/// <see cref="IIntegrationEventQueue"/>; the interceptor drains them and writes
/// them to the Outbox table at save time.
/// </summary>
public sealed class OutboxAccumulator : IIntegrationEventQueue
{
    private readonly List<(IIntegrationEvent Event, string? CorrelationId)> _events = [];

    public void Enqueue(IIntegrationEvent integrationEvent, string? correlationId = null) =>
        _events.Add((integrationEvent, correlationId));

    internal IReadOnlyList<(IIntegrationEvent Event, string? CorrelationId)> Drain()
    {
        var snapshot = _events.ToArray();
        _events.Clear();
        return snapshot;
    }
}
