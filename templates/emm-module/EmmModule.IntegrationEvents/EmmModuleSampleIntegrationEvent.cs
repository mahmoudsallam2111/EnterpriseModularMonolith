using BuildingBlocks.EventBus;

namespace EmmModule.IntegrationEvents;

/// <summary>
/// Published after a sample aggregate is created. Replace with module-specific
/// events. Lives in its own assembly so subscribers depend only on this thin contract,
/// not on Domain or Infrastructure.
/// </summary>
public sealed record EmmModuleSampleCreatedIntegrationEvent(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc) : IntegrationEvent;
