using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.FeatureFlags;
using BuildingBlocks.Application.Locking;
using BuildingBlocks.Application.Security;
using BuildingBlocks.EventBus;
using BuildingBlocks.EventBus.InProcess;
using BuildingBlocks.Infrastructure.Auditing;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Infrastructure.FeatureFlags;
using BuildingBlocks.Infrastructure.Locking;
using BuildingBlocks.MultiTenancy;
using BuildingBlocks.SharedKernel;
using BuildingBlocks.UnitOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace EnterpriseModularMonolith.Api.Composition;

/// <summary>
/// Single place that wires every cross-cutting concern that all modules depend on:
/// clock, current user, caching, feature flags, distributed locking, event bus, etc.
/// </summary>
internal static class InfrastructureBootstrap
{
    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        // Core platform primitives
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<BuildingBlocks.Application.Authorization.IPermissionService, DummyPermissionService>();
        services.AddSingleton<ITenantContext, NullTenantContext>();

        // Caching
        services.AddMemoryCache();
        services.AddScoped<ICacheService, InMemoryCacheService>();

        // Feature flags
        services.AddFeatureManagement();
        services.AddScoped<IFeatureFlags, MicrosoftFeatureFlags>();

        // Distributed lock (in-process default)
        services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();

        // Auditing
        services.AddScoped<IAuditLogger, LoggerAuditLogger>();

        // Ambient Unit of Work
        services.AddAmbientUnitOfWork();

        // Event bus + dispatcher
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<IIntegrationEventBus, InProcessIntegrationEventBus>();

        return services;
    }
}
