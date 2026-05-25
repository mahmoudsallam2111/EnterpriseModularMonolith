using System.Text.RegularExpressions;
using BuildingBlocks.Domain;

namespace Customers.Domain.Customers;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Email cannot be empty.", nameof(raw));
        var trimmed = raw.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(trimmed))
            throw new ArgumentException($"Invalid email format: {raw}", nameof(raw));
        return new Email(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
