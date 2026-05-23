using BuildingBlocks.EventBus;

namespace Users.IntegrationEvents;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string UserName,
    string Email,
    DateTimeOffset RegisteredAtUtc) : IntegrationEvent;

public sealed record UserLockedOutIntegrationEvent(
    Guid UserId,
    string Reason,
    DateTimeOffset LockedAtUtc) : IntegrationEvent;
