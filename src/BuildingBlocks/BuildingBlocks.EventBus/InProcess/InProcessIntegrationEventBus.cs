using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.EventBus.InProcess;

/// <summary>
/// Default integration event bus. Resolves all registered handlers for the event type
/// and invokes them sequentially. Backed by the outbox in real flows — direct publish
/// is used after the outbox processor has drained a message.
/// </summary>
public sealed class InProcessIntegrationEventBus : IIntegrationEventBus
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessIntegrationEventBus> _logger;

    public InProcessIntegrationEventBus(
        IServiceProvider serviceProvider,
        ILogger<InProcessIntegrationEventBus> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>().ToArray();

        if (handlers.Length == 0)
        {
            _logger.LogDebug("No handlers registered for {EventType}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(integrationEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Handler {Handler} failed for event {EventType} ({EventId})",
                    handler.GetType().Name, typeof(TEvent).Name, integrationEvent.EventId);
                throw;
            }
        }
    }
}
