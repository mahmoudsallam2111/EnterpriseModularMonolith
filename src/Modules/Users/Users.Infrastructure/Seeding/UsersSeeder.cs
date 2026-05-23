using BuildingBlocks.Infrastructure.Seeding;
using Users.Domain.Users;
using Users.Infrastructure.Persistence;

namespace Users.Infrastructure.Seeding;

public sealed class UsersSeeder : IDataSeeder
{
    private readonly UsersDbContext _context;
    private readonly IPasswordHasher _hasher;

    public UsersSeeder(UsersDbContext context, IPasswordHasher hasher)
    {
        _context = context;
        _hasher = hasher;
    }

    public int Order => 1;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_context.Roles.Any() || _context.Users.Any()) return;

        var admin = Role.Create("admin", "Has every permission.");
        foreach (var p in new[]
        {
            "customers.view", "customers.manage",
            "orders.view", "orders.manage",
            "users.view", "users.manage", "users.roles.manage"
        }) admin.GrantPermission(p);

        var viewer = Role.Create("viewer", "Read-only access.");
        foreach (var p in new[] { "customers.view", "orders.view", "users.view" })
            viewer.GrantPermission(p);

        await _context.Roles.AddRangeAsync(new[] { admin, viewer }, cancellationToken);

        var rootUser = User.Register(
            "admin",
            UserEmail.Create("admin@example.com"),
            _hasher.Hash("Admin#12345"));
        rootUser.AssignRole(admin.Id);

        await _context.Users.AddAsync(rootUser, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
