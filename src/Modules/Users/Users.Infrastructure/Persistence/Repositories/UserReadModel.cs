using Microsoft.EntityFrameworkCore;
using Users.Application.Users.Queries.GetMe;
using Users.Contracts;

namespace Users.Infrastructure.Persistence.Repositories;

internal sealed class UserReadModel : IUserReadModel
{
    private readonly UsersDbContext _context;
    public UserReadModel(UsersDbContext context) => _context = context;

    public async Task<UserSummaryDto?> GetSummaryAsync(Guid userId, CancellationToken cancellationToken)
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
}
