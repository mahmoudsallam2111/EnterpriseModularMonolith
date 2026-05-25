using BuildingBlocks.Auditing.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Auditing.Persistence;

/// <summary>
/// EF Core context for the audit database. Lives in its OWN physical database
/// (configured via the "Audit" connection string) — completely isolated from the
/// business <c>emm</c> database. Has its own migrations, retention policy, and
/// failure mode (a failed audit write must not break business operations).
///
/// Never add this DbContext to the regular UoW pipeline — audit writes run on
/// their own transaction, on their own connection, from a background drain.
/// </summary>
public sealed class AuditDbContext : DbContext
{
    public const string SchemaName = "audit";

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EntityChange> EntityChanges => Set<EntityChange>();
    public DbSet<EntityPropertyChange> EntityPropertyChanges => Set<EntityPropertyChange>();

    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
