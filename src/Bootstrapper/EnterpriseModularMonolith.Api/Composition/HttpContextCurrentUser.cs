using System.Security.Claims;
using BuildingBlocks.Application.Security;
using Microsoft.AspNetCore.Http;

namespace EnterpriseModularMonolith.Api.Composition;

/// <summary>
/// HttpContext-backed implementation of <see cref="ICurrentUser"/>. Lives in the
/// host so module application layers can stay framework-free.
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue("sub") ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : null;

    public string? UserName => Principal?.FindFirstValue("unique_name") ?? Principal?.Identity?.Name;
    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public IReadOnlyCollection<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).ToArray()
        ?? Array.Empty<string>();

    public Guid? TenantId =>
        Guid.TryParse(Principal?.FindFirstValue("tenant_id"), out var id) ? id : null;

    public string? GetClaim(string type) => Principal?.FindFirstValue(type);
}
