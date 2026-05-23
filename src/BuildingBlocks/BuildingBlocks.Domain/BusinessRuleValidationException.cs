namespace BuildingBlocks.Domain;

/// <summary>
/// Thrown when an aggregate detects that one of its invariants has been violated.
/// Translated to a Problem Details response by the presentation layer.
/// </summary>
public sealed class BusinessRuleValidationException : DomainException
{
    public IBusinessRule BrokenRule { get; }

    public BusinessRuleValidationException(IBusinessRule brokenRule)
        : base(brokenRule.Code, brokenRule.Message)
    {
        BrokenRule = brokenRule;
    }

    public override string ToString() => $"{BrokenRule.GetType().FullName}: {BrokenRule.Message}";
}
