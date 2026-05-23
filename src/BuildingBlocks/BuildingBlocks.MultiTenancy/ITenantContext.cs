namespace BuildingBlocks.MultiTenancy;

/// <summary>
/// Resolves the tenant for the current request/operation. The template is single-tenant
/// out of the box (returns null) — flip to a header/claim/host resolver when you need it.
/// </summary>
public interface ITenantContext
{
    TenantInfo? Current { get; }
}

public sealed record TenantInfo(Guid Id, string Name, IReadOnlyDictionary<string, string> Properties);

/// <summary>
/// Default no-op tenant resolver. Returns null — interpret as "single tenant".
/// </summary>
public sealed class NullTenantContext : ITenantContext
{
    public TenantInfo? Current => null;
}
