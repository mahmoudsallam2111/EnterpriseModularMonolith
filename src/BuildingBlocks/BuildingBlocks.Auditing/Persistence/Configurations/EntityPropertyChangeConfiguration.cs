using BuildingBlocks.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Auditing.Persistence.Configurations;

internal sealed class EntityPropertyChangeConfiguration : IEntityTypeConfiguration<EntityPropertyChange>
{
    public void Configure(EntityTypeBuilder<EntityPropertyChange> b)
    {
        b.ToTable("entity_property_changes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.EntityChangeId);
        b.Property(x => x.PropertyName).HasMaxLength(256).IsRequired();
        b.Property(x => x.PropertyType).HasMaxLength(256).IsRequired();
        b.Property(x => x.OriginalValue).HasMaxLength(1000);
        b.Property(x => x.NewValue).HasMaxLength(1000);

        b.HasIndex(x => x.EntityChangeId);
    }
}
