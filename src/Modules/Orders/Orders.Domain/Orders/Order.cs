using BuildingBlocks.Domain;
using Orders.Domain.Orders.Events;
using Orders.Domain.Orders.Rules;

namespace Orders.Domain.Orders;

/// <summary>
/// Order aggregate root. Owns its lines, enforces state-machine transitions,
/// and emits domain events on every meaningful transition.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>, IAuditableEntity
{
    private readonly List<OrderLine> _lines = [];

    /// <summary>Reference to the Customer aggregate — by id only, never by direct entity reference.</summary>
    public Guid CustomerId { get; private set; }
    public string Currency { get; private set; } = default!;
    public OrderStatus Status { get; private set; }
    public DateTimeOffset? PlacedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();
    public Money Total => _lines
        .Select(l => l.LineTotal)
        .Aggregate(Money.Zero(Currency), (a, b) => a.Add(b));

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Order() { }

    private Order(OrderId id, Guid customerId, string currency) : base(id)
    {
        CustomerId = customerId;
        Currency = currency;
        Status = OrderStatus.Draft;
    }

    public static Order Draft(Guid customerId, string currency)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId required.", nameof(customerId));
        var order = new Order(OrderId.New(), customerId, currency.ToUpperInvariant());
        order.RaiseDomainEvent(new OrderDraftedDomainEvent(order.Id, customerId));
        return order;
    }

    public OrderLine AddLine(string sku, string name, int quantity, decimal unitPrice)
    {
        CheckRule(new OrderMustBeInDraftToModify(Status));
        var existing = _lines.FirstOrDefault(l => l.ProductSku == sku);
        if (existing is not null)
        {
            existing.ChangeQuantity(existing.Quantity + quantity);
            return existing;
        }
        var line = new OrderLine(OrderLineId.New(), sku, name, quantity, Money.Of(unitPrice, Currency));
        _lines.Add(line);
        RaiseDomainEvent(new OrderLineAddedDomainEvent(Id, line.Id, sku, quantity));
        return line;
    }

    public void RemoveLine(OrderLineId lineId)
    {
        CheckRule(new OrderMustBeInDraftToModify(Status));
        var line = _lines.FirstOrDefault(l => l.Id == lineId);
        if (line is null) return;
        _lines.Remove(line);
    }

    public void Place(DateTimeOffset whenUtc)
    {
        CheckRule(new OrderMustBeInDraftToModify(Status));
        CheckRule(new OrderMustHaveAtLeastOneLineToPlace(_lines.Count));
        Status = OrderStatus.Placed;
        PlacedAtUtc = whenUtc;
        var total = Total;
        RaiseDomainEvent(new OrderPlacedDomainEvent(Id, CustomerId, total.Amount, total.Currency));
    }

    public void MarkPaid()
    {
        if (Status != OrderStatus.Placed)
            throw new InvalidOperationException($"Cannot mark paid: order is {Status}.");
        Status = OrderStatus.Paid;
        RaiseDomainEvent(new OrderPaidDomainEvent(Id, Total.Amount, Currency));
    }

    public void MarkShipped()
    {
        if (Status != OrderStatus.Paid)
            throw new InvalidOperationException($"Cannot ship: order is {Status}.");
        Status = OrderStatus.Shipped;
    }

    public void Complete(DateTimeOffset whenUtc)
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException($"Cannot complete: order is {Status}.");
        Status = OrderStatus.Completed;
        CompletedAtUtc = whenUtc;
    }

    public void Cancel(string reason, DateTimeOffset whenUtc)
    {
        CheckRule(new OrderCannotBeCancelledOnceCompleted(Status));
        Status = OrderStatus.Cancelled;
        CancelledAtUtc = whenUtc;
        CancellationReason = reason;
        RaiseDomainEvent(new OrderCancelledDomainEvent(Id, CustomerId, reason));
    }
}
