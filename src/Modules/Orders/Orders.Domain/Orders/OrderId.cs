using BuildingBlocks.Domain;

namespace Orders.Domain.Orders;

public sealed record OrderId(Guid Value) : StronglyTypedId<OrderId>(Value)
{
    public static OrderId New() => new(Guid.NewGuid());
    public static OrderId From(Guid value) => new(value);
}

public sealed record OrderLineId(Guid Value) : StronglyTypedId<OrderLineId>(Value)
{
    public static OrderLineId New() => new(Guid.NewGuid());
}
