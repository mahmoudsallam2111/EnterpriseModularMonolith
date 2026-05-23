using BuildingBlocks.Domain;

namespace Users.Domain.Users;

/// <summary>
/// Role aggregate. Owns its permission set. Strategic choice: Role is a small
/// aggregate; User holds RoleIds rather than Role references to keep aggregate
/// boundaries clean and concurrency tight.
/// </summary>
public sealed class Role : AggregateRoot<RoleId>, IAuditableEntity
{
    private readonly HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public IReadOnlyCollection<string> Permissions => _permissions;

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    private Role() { }

    private Role(RoleId id, string name, string? description) : base(id)
    {
        Name = name;
        Description = description;
    }

    public static Role Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Role name required.", nameof(name));
        if (name.Length > 100) throw new ArgumentException("Role name too long.", nameof(name));
        return new Role(RoleId.New(), name.Trim(), description?.Trim());
    }

    public void GrantPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
            throw new ArgumentException("Permission required.", nameof(permission));
        _permissions.Add(permission.Trim());
    }

    public void RevokePermission(string permission) => _permissions.Remove(permission);

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        Name = name.Trim();
    }
}
