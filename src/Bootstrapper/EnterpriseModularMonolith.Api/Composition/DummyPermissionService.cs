using BuildingBlocks.Application.Authorization;

namespace EnterpriseModularMonolith.Api.Composition;

internal sealed class DummyPermissionService : IPermissionService
{
    public Task<bool> HasPermissionAsync(Guid userId, string permission, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true); // Always allow since auth is disabled
    }

    public Task<IReadOnlyCollection<string>> GetPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
    }
}
