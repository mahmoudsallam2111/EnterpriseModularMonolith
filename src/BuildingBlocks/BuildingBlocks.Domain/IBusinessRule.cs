namespace BuildingBlocks.Domain;

/// <summary>
/// A named invariant that can be checked against an aggregate state.
/// Inspired by Kamil Grzybek's Modular Monolith with DDD reference implementation.
/// </summary>
public interface IBusinessRule
{
    string Message { get; }
    string Code { get; }
    bool IsBroken();
}
