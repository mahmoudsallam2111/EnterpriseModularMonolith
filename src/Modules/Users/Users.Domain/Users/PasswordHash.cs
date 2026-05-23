using BuildingBlocks.Domain;

namespace Users.Domain.Users;

/// <summary>
/// Wraps an opaque password hash. The hashing algorithm itself is injected via
/// <see cref="IPasswordHasher"/> — the domain never knows BCrypt exists.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    public string Value { get; }
    private PasswordHash(string value) => Value = value;

    public static PasswordHash FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Password hash cannot be empty.", nameof(hash));
        return new PasswordHash(hash);
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
}

/// <summary>
/// Domain-defined hasher contract. The infrastructure layer implements it (using BCrypt).
/// </summary>
public interface IPasswordHasher
{
    PasswordHash Hash(string password);
    bool Verify(string password, PasswordHash hash);
}
