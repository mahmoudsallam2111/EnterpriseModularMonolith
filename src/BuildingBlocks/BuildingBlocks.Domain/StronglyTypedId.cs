namespace BuildingBlocks.Domain;

/// <summary>
/// Base record for strongly typed GUID identifiers. Prevents accidentally passing
/// e.g. a CustomerId where an OrderId is expected.
/// </summary>
public abstract record StronglyTypedId<T>(Guid Value)
    where T : StronglyTypedId<T>
{
    public override string ToString() => Value.ToString();
    public static implicit operator Guid(StronglyTypedId<T> id) => id.Value;
}
