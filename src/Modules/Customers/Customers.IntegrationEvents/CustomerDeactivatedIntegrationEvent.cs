using BuildingBlocks.EventBus;

namespace Customers.IntegrationEvents;

public sealed record CustomerDeactivatedIntegrationEvent(
    Guid CustomerId,
    string Reason,
    DateTimeOffset DeactivatedAtUtc) : IntegrationEvent;
