namespace Users.Application.Auth;

/// <summary>
/// Issues JWTs for authenticated users. The Domain doesn't know about JWTs at all —
/// that's an infrastructure concern, configured via JwtOptions in the host.
/// </summary>
public interface ITokenIssuer
{
    AuthToken Issue(
        Guid userId,
        string userName,
        string email,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);
}

public sealed record AuthToken(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType = "Bearer");
