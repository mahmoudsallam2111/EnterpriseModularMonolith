using BuildingBlocks.Application.FeatureFlags;
using Microsoft.FeatureManagement;

namespace BuildingBlocks.Infrastructure.FeatureFlags;

/// <summary>
/// Adapter over Microsoft.Extensions.FeatureManagement so the rest of the app
/// only depends on our thin IFeatureFlags abstraction.
/// </summary>
public sealed class MicrosoftFeatureFlags : IFeatureFlags
{
    private readonly IFeatureManager _featureManager;

    public MicrosoftFeatureFlags(IFeatureManager featureManager) => _featureManager = featureManager;

    public Task<bool> IsEnabledAsync(string feature, CancellationToken cancellationToken = default) =>
        _featureManager.IsEnabledAsync(feature);
}
