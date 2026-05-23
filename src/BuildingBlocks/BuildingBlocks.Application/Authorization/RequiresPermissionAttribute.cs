namespace BuildingBlocks.Application.Authorization;

/// <summary>
/// Declarative permission requirement applied to a command or query.
/// Enforced by AuthorizationBehavior in the MediatR pipeline.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequiresPermissionAttribute : Attribute
{
    public RequiresPermissionAttribute(string permission) => Permission = permission;
    public string Permission { get; }
}
