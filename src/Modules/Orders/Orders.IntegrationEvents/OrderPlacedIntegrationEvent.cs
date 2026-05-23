using BuildingBlocks.EventBus;

namespace Orders.IntegrationEvents;

public sealed record OrderPlacedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    int LineCount,
    DateTimeOffset PlacedAtUtc) : IntegrationEvent;

public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    DateTimeOffset CancelledAtUtc) : IntegrationEvent;
