using System.Collections.ObjectModel;

namespace BuildingBlocks.Auditing.Entities;

/// <summary>
/// One row per (entity touched within an operation). Each carries N
/// <see cref="EntityPropertyChange"/> diffs.
/// </summary>
public sealed class EntityChange
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AuditLogId { get; set; }

    public DateTimeOffset ChangeTime { get; set; } = DateTimeOffset.UtcNow;
    public EntityChangeType ChangeType { get; set; }

    /// <summary>Stringified primary key. Works for guid/int/composite alike.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Full type name of the entity (namespace + name).</summary>
    public string EntityType { get; set; } = string.Empty;

    public Guid? EntityTenantId { get; set; }

    /// <summary>Owning module ("Customers", "Orders", "Users"). Derived from the DbContext.</summary>
    public string? Module { get; set; }

    public Collection<EntityPropertyChange> PropertyChanges { get; } = [];
}
