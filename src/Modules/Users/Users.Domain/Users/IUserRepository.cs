using BuildingBlocks.Application.Persistence;

namespace Users.Domain.Users;

public interface IUserRepository : IWriteRepository<User, UserId>
{
    Task<User?> FindByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(UserEmail email, CancellationToken cancellationToken = default);
    Task<bool> UserNameTakenAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> EmailTakenAsync(UserEmail email, CancellationToken cancellationToken = default);
}

public interface IRoleRepository : IWriteRepository<Role, RoleId>
{
    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);
}
