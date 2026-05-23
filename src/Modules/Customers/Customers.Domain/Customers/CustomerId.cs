using BuildingBlocks.Domain;

namespace Customers.Domain.Customers;

/// <summary>
/// Strongly typed identifier for a <see cref="Customer"/> aggregate.
/// Prevents accidental mixing with OrderId / UserId at the type level.
/// </summary>
public sealed record CustomerId(Guid Value) : StronglyTypedId<CustomerId>(Value)
{
    public static CustomerId New() => new(Guid.NewGuid());
    public static CustomerId From(Guid value) => new(value);
}
