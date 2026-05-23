using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");
        b.HasKey(o => o.Id);

        b.Property(o => o.Id)
            .HasConversion(id => id.Value, value => OrderId.From(value))
            .ValueGeneratedNever();

        b.Property(o => o.CustomerId).IsRequired();
        b.HasIndex(o => o.CustomerId);

        b.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        b.Property(o => o.Status).HasConversion<int>();
        b.Property(o => o.CancellationReason).HasMaxLength(500);

        b.Property<uint>("Version")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        b.OwnsMany(o => o.Lines, l =>
        {
            l.ToTable("order_lines");
            l.WithOwner().HasForeignKey("order_id");
            l.HasKey(x => x.Id);
            l.Property(x => x.Id)
                .HasConversion(id => id.Value, value => new OrderLineId(value))
                .ValueGeneratedNever();
            l.Property(x => x.ProductSku).HasMaxLength(64).IsRequired();
            l.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
            l.Property(x => x.Quantity).IsRequired();
            l.OwnsOne(x => x.UnitPrice, m =>
            {
                m.Property(p => p.Amount).HasColumnName("unit_price_amount").HasPrecision(18, 4);
                m.Property(p => p.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
            });
        });

        b.Ignore(o => o.Total);
        b.Ignore(o => o.DomainEvents);
    }
}
