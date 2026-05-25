using BuildingBlocks.Application.Security;

namespace EnterpriseModularMonolith.Migrator;

internal sealed class MigratorCurrentUser : ICurrentUser
{
    private static readonly Guid MigratorUserId = Guid.Parse("00000000-0000-0000-0000-000000000999");

    public bool IsAuthenticated => true;
    public Guid? UserId => MigratorUserId;
    public string? UserName => "database-migrator";
    public string? Email => null;
    public IReadOnlyCollection<string> Roles => ["System"];
    public IReadOnlyCollection<string> Permissions => ["*"];
    public Guid? TenantId => null;
    public string? GetClaim(string type) => null;
}
