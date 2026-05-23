using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Users.Domain.Users;

namespace Users.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).HasConversion(id => id.Value, value => UserId.From(value)).ValueGeneratedNever();

        b.Property(u => u.UserName).HasMaxLength(100).IsRequired();
        b.HasIndex(u => u.UserName).IsUnique();

        b.OwnsOne(u => u.Email, e =>
        {
            e.Property(p => p.Value).HasColumnName("email").HasMaxLength(254).IsRequired();
            e.HasIndex(p => p.Value).IsUnique();
        });

        b.OwnsOne(u => u.Password, p =>
        {
            p.Property(x => x.Value).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
        });

        b.Property(u => u.IsLockedOut);
        b.Property(u => u.LockoutReason).HasMaxLength(500);
        b.Property(u => u.FailedLoginAttempts);

        b.Property<uint>("Version")
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        b.Property(u => u.IsDeleted);
        b.HasQueryFilter(u => !u.IsDeleted);

        // RoleIds as a join table
        b.OwnsMany<RoleAssignment>("RoleAssignments", a =>
        {
            a.ToTable("user_roles");
            a.WithOwner().HasForeignKey("user_id");
            a.HasKey("user_id", "role_id");
            a.Property<Guid>("role_id");
        });

        b.Ignore(u => u.DomainEvents);
        b.Ignore(u => u.RoleIds);
    }
}

internal sealed class RoleAssignment { /* shadow type to map join table */ }

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).HasConversion(id => id.Value, value => RoleId.From(value)).ValueGeneratedNever();
        b.Property(r => r.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(r => r.Name).IsUnique();
        b.Property(r => r.Description).HasMaxLength(500);

        b.Property<uint>("Version")
            .HasColumnName("xmin").HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();

        b.Property<HashSet<string>>("_permissions")
            .HasColumnName("permissions")
            .HasColumnType("text[]")
            .HasField("_permissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.Ignore(r => r.Permissions);
        b.Ignore(r => r.DomainEvents);
    }
}
