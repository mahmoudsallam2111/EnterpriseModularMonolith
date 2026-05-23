using System.Text.RegularExpressions;
using BuildingBlocks.Domain;

namespace Users.Domain.Users;

/// <summary>
/// Email value object scoped to the Users bounded context. Notice we don't share the
/// Customers.Email type — each bounded context owns its own model even when names overlap.
/// </summary>
public sealed partial class UserEmail : ValueObject
{
    public string Value { get; }
    private UserEmail(string value) => Value = value;

    public static UserEmail Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new ArgumentException("Email cannot be empty.", nameof(raw));
        var trimmed = raw.Trim().ToLowerInvariant();
        if (!Regex().IsMatch(trimmed))
            throw new ArgumentException($"Invalid email format: {raw}", nameof(raw));
        return new UserEmail(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex Regex();
}
