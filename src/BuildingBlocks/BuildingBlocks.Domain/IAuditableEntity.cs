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
/// converts EF Remove() into a flag flip; ModuleDbContext auto-applies a global
/// query filter (gated by IDataFilter&lt;ISoftDeletable&gt;) to hide them on reads.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}

/// <summary>
/// Marker for entities partitioned by tenant. The ModuleDbContext auto-applies a
/// global query filter (gated by IDataFilter&lt;IMultiTenantEntity&gt;) that limits
/// reads to the current tenant — and the auditing interceptor stamps TenantId on save.
/// </summary>
public interface IMultiTenantEntity
{
    Guid? TenantId { get; set; }
}
