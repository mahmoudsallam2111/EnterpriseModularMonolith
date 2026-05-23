using BuildingBlocks.Domain;

namespace Customers.Domain.Customers;

public sealed class PersonName : ValueObject
{
    public string First { get; }
    public string Last { get; }

    private PersonName(string first, string last) { First = first; Last = last; }

    public static PersonName Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first)) throw new ArgumentException("First name required.", nameof(first));
        if (string.IsNullOrWhiteSpace(last)) throw new ArgumentException("Last name required.", nameof(last));
        if (first.Length > 100) throw new ArgumentException("First name too long.", nameof(first));
        if (last.Length > 100) throw new ArgumentException("Last name too long.", nameof(last));
        return new PersonName(first.Trim(), last.Trim());
    }

    public string Full => $"{First} {Last}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return First;
        yield return Last;
    }

    public override string ToString() => Full;
}
