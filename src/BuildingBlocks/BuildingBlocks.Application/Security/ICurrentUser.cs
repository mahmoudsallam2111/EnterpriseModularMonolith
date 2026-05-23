namespace BuildingBlocks.Application.Security;

/// <summary>
/// Application-layer abstraction over the authenticated principal so that handlers
/// never depend on HttpContext, ClaimsPrincipal, or any presentation primitive.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
    Guid? TenantId { get; }
    string? GetClaim(string type);
}
