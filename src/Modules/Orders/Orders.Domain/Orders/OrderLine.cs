using BuildingBlocks.Domain;

namespace Orders.Domain.Orders;

/// <summary>
/// A line on an order. Belongs to <see cref="Order"/> — outside code cannot construct
/// one directly; lines are added/removed through the aggregate root.
/// </summary>
public sealed class OrderLine : Entity<OrderLineId>
{
    public string ProductSku { get; private set; } = default!;
    public string ProductName { get; private set; } = default!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = default!;

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderLine() { }

    internal OrderLine(OrderLineId id, string productSku, string productName, int quantity, Money unitPrice)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(productSku)) throw new ArgumentException("Sku required.", nameof(productSku));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    internal void ChangeQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Quantity = quantity;
    }
}
