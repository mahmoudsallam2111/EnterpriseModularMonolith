namespace BuildingBlocks.Application.FeatureFlags;

/// <summary>
/// Thin abstraction over the configured feature flag provider. The default
/// implementation wraps Microsoft.Extensions.FeatureManagement.
/// </summary>
public interface IFeatureFlags
{
    Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default);
}
