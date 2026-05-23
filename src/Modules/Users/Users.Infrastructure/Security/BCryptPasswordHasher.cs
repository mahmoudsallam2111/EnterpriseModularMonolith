using Users.Domain.Users;

namespace Users.Infrastructure.Security;

/// <summary>
/// BCrypt-backed password hasher. The domain knows nothing about BCrypt — it
/// receives an opaque <see cref="PasswordHash"/> through the <see cref="IPasswordHasher"/>
/// abstraction.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public PasswordHash Hash(string password) =>
        PasswordHash.FromHash(BCrypt.Net.BCrypt.HashPassword(password, WorkFactor));

    public bool Verify(string password, PasswordHash hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash.Value);
}
