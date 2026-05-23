namespace BuildingBlocks.Application.Authorization;

/// <summary>
/// Strategy for resolving whether a user holds a permission. Implementation
/// lives in the Users module (which owns roles/permissions); other modules
/// only depend on this contract.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
