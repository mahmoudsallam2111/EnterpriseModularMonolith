using BuildingBlocks.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Users.Contracts;

namespace Users.Infrastructure.PublicApi;

internal sealed class UsersApi : IUsersApi
{
    private readonly Persistence.UsersDbContext _context;
    private readonly IPermissionService _permissions;

    public UsersApi(Persistence.UsersDbContext context, IPermissionService permissions)
    {
        _context = context;
        _permissions = permissions;
    }

    public async Task<UserSummaryDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id.Value == userId, cancellationToken);
        if (user is null) return null;

        var roleIds = user.RoleIds.Select(r => r.Value).ToArray();
        var roles = await _context.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id.Value))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        return new UserSummaryDto(user.Id.Value, user.UserName, user.Email.Value, !user.IsLockedOut, roles);
    }

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.Users.AsNoTracking().AnyAsync(u => u.Id.Value == userId, cancellationToken);

    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _permissions.GetPermissionsAsync(userId, cancellationToken);
}
