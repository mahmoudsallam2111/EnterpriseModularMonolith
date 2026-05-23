using BuildingBlocks.Domain;

namespace Users.Domain.Users;

public sealed record UserId(Guid Value) : StronglyTypedId<UserId>(Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public static UserId From(Guid value) => new(value);
}

public sealed record RoleId(Guid Value) : StronglyTypedId<RoleId>(Value)
{
    public static RoleId New() => new(Guid.NewGuid());
    public static RoleId From(Guid value) => new(value);
}
