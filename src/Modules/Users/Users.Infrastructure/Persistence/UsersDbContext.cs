using BuildingBlocks.EventBus;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Users.Domain.Users;

namespace Users.Infrastructure.Persistence;

public sealed class UsersDbContext : ModuleDbContext
{
    public const string SchemaName = "users";
    public override string Schema => SchemaName;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    public UsersDbContext(
        DbContextOptions<UsersDbContext> options,
        IDomainEventDispatcher domainEventDispatcher,
        ILogger<UsersDbContext> logger)
        : base(options, domainEventDispatcher, logger) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
