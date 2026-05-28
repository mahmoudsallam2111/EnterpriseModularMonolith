using EmmModule.Domain.EmmModules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmmModule.Infrastructure.Persistence.Configurations;

internal sealed class EmmModuleSampleConfiguration : IEntityTypeConfiguration<EmmModuleSample>
{
    public void Configure(EntityTypeBuilder<EmmModuleSample> b)
    {
        b.ToTable("samples");
        b.HasKey(s => s.Id);

        b.Property(s => s.Id)
            .HasConversion(id => id.Value, value => EmmModuleSampleId.From(value))
            .ValueGeneratedNever();

        b.Property(s => s.Name).HasMaxLength(200).IsRequired();

        // Postgres concurrency token via xmin.
        b.Property<uint>("Version")
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        b.Property(s => s.CreatedAt);
        b.Property(s => s.UpdatedAt);
        b.Property(s => s.IsDeleted);
        b.Property(s => s.DeletedAt);

        b.Ignore(s => s.DomainEvents);
    }
}
