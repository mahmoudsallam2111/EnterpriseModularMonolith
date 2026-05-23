using BuildingBlocks.EventBus;

namespace Customers.IntegrationEvents;

/// <summary>
/// Published after a customer has been successfully registered. Consumed by other
/// modules (e.g. Users could provision an account, Orders could pre-warm a cache).
/// </summary>
public sealed record CustomerRegisteredIntegrationEvent(
    Guid CustomerId,
    string FullName,
    string Email,
    DateTimeOffset RegisteredAtUtc) : IntegrationEvent;
