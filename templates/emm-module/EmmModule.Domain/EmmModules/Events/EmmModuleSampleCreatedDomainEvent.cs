using BuildingBlocks.Domain;

namespace EmmModule.Domain.EmmModules.Events;

public sealed record EmmModuleSampleCreatedDomainEvent(EmmModuleSampleId Id, string Name) : DomainEvent;
