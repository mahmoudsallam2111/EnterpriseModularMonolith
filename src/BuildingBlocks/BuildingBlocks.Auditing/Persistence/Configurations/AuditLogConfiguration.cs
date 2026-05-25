using BuildingBlocks.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingBlocks.Auditing.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Action).HasMaxLength(256).IsRequired();
        b.Property(x => x.ServiceName).HasMaxLength(512);
        b.Property(x => x.MethodName).HasMaxLength(256);
        b.Property(x => x.Parameters).HasColumnType("jsonb");
        b.Property(x => x.ReturnValue).HasColumnType("jsonb");

        b.Property(x => x.HttpMethod).HasMaxLength(16);
        b.Property(x => x.Url).HasMaxLength(2048);
        b.Property(x => x.ClientIp).HasMaxLength(64);
        b.Property(x => x.ClientName).HasMaxLength(256);
        b.Property(x => x.BrowserInfo).HasMaxLength(512);
        b.Property(x => x.CorrelationId).HasMaxLength(128);
        b.Property(x => x.UserName).HasMaxLength(256);
        b.Property(x => x.Exception).HasColumnType("text");

        b.Property(x => x.ExecutionTime);
        b.Property(x => x.ExecutionDurationMs);

        b.HasIndex(x => x.ExecutionTime);
        b.HasIndex(x => new { x.UserId, x.ExecutionTime });
        b.HasIndex(x => x.CorrelationId);
        b.HasIndex(x => x.Action);

        b.HasMany(x => x.EntityChanges)
            .WithOne()
            .HasForeignKey(c => c.AuditLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
