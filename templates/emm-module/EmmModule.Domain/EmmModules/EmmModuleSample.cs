using BuildingBlocks.Domain;
using EmmModule.Domain.EmmModules.Events;
using EmmModule.Domain.EmmModules.Rules;

namespace EmmModule.Domain.EmmModules;

/// <summary>
/// Sample aggregate root for the EmmModule module. Replace with real domain types.
/// Implements <see cref="IAuditableEntity"/> and <see cref="ISoftDeletable"/> so the
/// auditing interceptor and the global soft-delete query filter pick it up automatically.
/// </summary>
public sealed class EmmModuleSample
    : AggregateRoot<EmmModuleSampleId>, IAuditableEntity, ISoftDeletable, IAuditedEntity
{
    public string Name { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private EmmModuleSample() { }

    private EmmModuleSample(EmmModuleSampleId id, string name) : base(id) => Name = name;

    public static EmmModuleSample Create(string name)
    {
        CheckRule(new EmmModuleSampleNameMustBeProvided(name));
        var entity = new EmmModuleSample(EmmModuleSampleId.New(), name.Trim());
        entity.RaiseDomainEvent(new EmmModuleSampleCreatedDomainEvent(entity.Id, entity.Name));
        return entity;
    }

    public void Rename(string newName)
    {
        CheckRule(new EmmModuleSampleNameMustBeProvided(newName));
        Name = newName.Trim();
    }
}
