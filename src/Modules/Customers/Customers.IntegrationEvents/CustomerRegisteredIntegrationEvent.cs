using BuildingBlocks.EventBus;

namespace Customers.IntegrationEvents;

public sealed record CustomerRegisteredIntegrationEvent(
    Guid CustomerId,
    string FullName,
    string Email,
    DateTimeOffset RegisteredAtUtc) : IntegrationEvent;
