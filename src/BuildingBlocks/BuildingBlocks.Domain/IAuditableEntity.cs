namespace BuildingBlocks.Domain;

/// <summary>
/// Marker for entities that opt into automatic auditing (CreatedAt/By, UpdatedAt/By).
/// The infrastructure-side AuditingInterceptor stamps these on save. Lives in Domain
/// so aggregates can implement it without taking a dependency on Infrastructure.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Marker for entities that participate in soft delete. The SoftDeleteInterceptor
/// converts EF Remove() into a flag flip; a global query filter hides them on reads.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
