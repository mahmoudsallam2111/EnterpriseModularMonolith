using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("customers");
        b.HasKey(c => c.Id);

        b.Property(c => c.Id)
            .HasConversion(id => id.Value, value => CustomerId.From(value))
            .ValueGeneratedNever();

        b.Property(c => c.Status).HasConversion<int>();

        // Concurrency token mapped to Postgres xmin.
        b.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Email value object — owned, indexed unique
        b.OwnsOne(c => c.Email, e =>
        {
            e.Property(p => p.Value)
                .HasColumnName("email")
                .HasMaxLength(254)
                .IsRequired();
            e.HasIndex(p => p.Value).IsUnique();
        });

        // Name VO — owned, inlined as two columns
        b.OwnsOne(c => c.Name, n =>
        {
            n.Property(p => p.First).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            n.Property(p => p.Last).HasColumnName("last_name").HasMaxLength(100).IsRequired();
        });

        // Addresses — owned collection, separate table
        b.OwnsMany(c => c.Addresses, a =>
        {
            a.ToTable("customer_addresses");
            a.WithOwner().HasForeignKey("customer_id");
            a.Property<int>("Id").ValueGeneratedOnAdd();
            a.HasKey("Id");
            a.Property(p => p.Street).HasMaxLength(200).IsRequired();
            a.Property(p => p.City).HasMaxLength(100).IsRequired();
            a.Property(p => p.PostalCode).HasMaxLength(20).IsRequired();
            a.Property(p => p.Country).HasMaxLength(100).IsRequired();
        });

        b.HasQueryFilter(c => !c.IsDeleted);

        b.Property(c => c.CreatedAt);
        b.Property(c => c.UpdatedAt);
        b.Property(c => c.IsDeleted);
        b.Property(c => c.DeletedAt);

        b.Ignore(c => c.DomainEvents);
    }
}
