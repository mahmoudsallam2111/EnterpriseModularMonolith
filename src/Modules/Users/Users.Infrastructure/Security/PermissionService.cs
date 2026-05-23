using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Caching;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Persistence;

namespace Users.Infrastructure.Security;

/// <summary>
/// The single source of truth for user permissions. Reads the user's roles and
/// their permissions, caching the resulting set briefly to amortise the lookup.
/// The Users module owns this — every other module consumes it through the
/// <see cref="IPermissionService"/> interface in BuildingBlocks.Application.
/// </summary>
internal sealed class PermissionService : IPermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly UsersDbContext _context;
    private readonly ICacheService _cache;

    public PermissionService(UsersDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        var perms = await GetPermissionsAsync(userId, cancellationToken);
        return perms.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var key = $"perm:{userId:N}";
        return await _cache.GetOrAddAsync(key, async _ =>
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id.Value == userId, cancellationToken);
            if (user is null) return Array.Empty<string>();

            var roleIds = user.RoleIds.Select(r => r.Value).ToArray();
            if (roleIds.Length == 0) return Array.Empty<string>();

            var roles = await _context.Roles.AsNoTracking()
                .Where(r => roleIds.Contains(r.Id.Value))
                .ToListAsync(cancellationToken);

            return roles.SelectMany(r => r.Permissions)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }, CacheTtl, cancellationToken) ?? Array.Empty<string>();
    }
}
