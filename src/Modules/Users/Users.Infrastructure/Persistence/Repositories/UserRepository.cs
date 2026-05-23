using BuildingBlocks.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Users.Domain.Users;

namespace Users.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository
    : EfWriteRepository<UsersDbContext, User, UserId>, IUserRepository
{
    public UserRepository(UsersDbContext context) : base(context) { }

    public Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default) =>
        Context.Users.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

    public Task<User?> FindByEmailAsync(UserEmail email, CancellationToken cancellationToken = default) =>
        Context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> UserNameTakenAsync(string userName, CancellationToken cancellationToken = default) =>
        Context.Users.AnyAsync(u => u.UserName == userName, cancellationToken);

    public Task<bool> EmailTakenAsync(UserEmail email, CancellationToken cancellationToken = default) =>
        Context.Users.AnyAsync(u => u.Email == email, cancellationToken);
}

internal sealed class RoleRepository
    : EfWriteRepository<UsersDbContext, Role, RoleId>, IRoleRepository
{
    public RoleRepository(UsersDbContext context) : base(context) { }

    public Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
}
