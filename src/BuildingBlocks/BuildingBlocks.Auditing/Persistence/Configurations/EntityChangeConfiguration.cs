using BuildingBlocks.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Auditing.Persistence.Configurations;

internal sealed class EntityChangeConfiguration : IEntityTypeConfiguration<EntityChange>
{
    public void Configure(EntityTypeBuilder<EntityChange> b)
    {
        b.ToTable("entity_changes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.AuditLogId);
        b.Property(x => x.ChangeType).HasConversion<short>();
        b.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(512).IsRequired();
        b.Property(x => x.Module).HasMaxLength(128);

        b.HasIndex(x => x.AuditLogId);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.ChangeTime);

        b.HasMany(x => x.PropertyChanges)
            .WithOne()
            .HasForeignKey(p => p.EntityChangeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
