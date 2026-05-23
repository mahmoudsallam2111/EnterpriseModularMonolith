namespace Users.Infrastructure.Security;

/// <summary>
/// Bound from the "Jwt" config section. The signing key MUST come from a secret
/// store in any environment above local dev.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "EnterpriseModularMonolith";
    public string Audience { get; set; } = "EnterpriseModularMonolith";
    public string SigningKey { get; set; } = default!;
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
