using BuildingBlocks.Domain;

namespace Orders.Domain.Orders.Events;

public sealed record OrderDraftedDomainEvent(OrderId OrderId, Guid CustomerId) : DomainEvent;
public sealed record OrderLineAddedDomainEvent(OrderId OrderId, OrderLineId OrderLineId, string Sku, int Quantity) : DomainEvent;
public sealed record OrderPlacedDomainEvent(OrderId OrderId, Guid CustomerId, decimal Total, string Currency) : DomainEvent;
public sealed record OrderCancelledDomainEvent(OrderId OrderId, Guid CustomerId, string Reason) : DomainEvent;
public sealed record OrderPaidDomainEvent(OrderId OrderId, decimal Amount, string Currency) : DomainEvent;
