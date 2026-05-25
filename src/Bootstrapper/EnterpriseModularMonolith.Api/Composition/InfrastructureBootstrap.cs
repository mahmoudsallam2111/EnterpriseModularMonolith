using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.FeatureFlags;
using BuildingBlocks.Application.Locking;
using BuildingBlocks.Application.Security;
using BuildingBlocks.EventBus;
using BuildingBlocks.EventBus.InProcess;
using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Infrastructure.UnitOfWork;
using BuildingBlocks.Infrastructure.Auditing;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Infrastructure.FeatureFlags;
using BuildingBlocks.Infrastructure.Locking;
using BuildingBlocks.MultiTenancy;
using BuildingBlocks.Infrastructure.Redis;
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
    private const string InMemoryProvider = "InMemory";
    private const string RedisProvider = "Redis";

    public static IServiceCollection AddPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        // Core platform primitives
        services.AddHttpContextAccessor();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddScoped<BuildingBlocks.Application.Authorization.IPermissionService, DummyPermissionService>();
        services.AddSingleton<ITenantContext, NullTenantContext>();

        services.AddConfiguredCache(configuration);

        // Feature flags
        services.AddFeatureManagement();
        services.AddScoped<IFeatureFlags, MicrosoftFeatureFlags>();

        services.AddConfiguredDistributedLock(configuration);

        // Auditing
        services.AddScoped<IAuditLogger, LoggerAuditLogger>();

        // Ambient Unit of Work
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");
        services.AddScoped(_ => new SharedModuleDbConnection(connectionString));
        services.AddAmbientUnitOfWork();
        services.AddScoped<IUnitOfWorkFactory, EfCoreUnitOfWorkFactory>();

        // Event bus + dispatcher
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<IIntegrationEventBus, InProcessIntegrationEventBus>();

        return services;
    }

    private static IServiceCollection AddConfiguredCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Platform:Cache:Provider"] ?? InMemoryProvider;

        if (IsProvider(provider, InMemoryProvider))
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
            return services;
        }

        if (IsProvider(provider, RedisProvider))
        {
            services.AddRedisCacheService(
                GetRequiredRedisConnectionString(configuration),
                configuration["Platform:Cache:Redis:KeyPrefix"] ?? "emm:cache:");
            return services;
        }

        throw new InvalidOperationException(
            $"Unsupported cache provider '{provider}'. Supported providers: {InMemoryProvider}, {RedisProvider}.");
    }

    private static IServiceCollection AddConfiguredDistributedLock(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["Platform:DistributedLock:Provider"] ?? InMemoryProvider;

        if (IsProvider(provider, InMemoryProvider))
        {
            services.AddSingleton<IDistributedLock, InMemoryDistributedLock>();
            return services;
        }

        if (IsProvider(provider, RedisProvider))
        {
            services.AddRedisDistributedLock(
                GetRequiredRedisConnectionString(configuration),
                configuration["Platform:DistributedLock:Redis:KeyPrefix"] ?? "emm:locks:");
            return services;
        }

        throw new InvalidOperationException(
            $"Unsupported distributed lock provider '{provider}'. Supported providers: {InMemoryProvider}, {RedisProvider}.");
    }

    private static string GetRequiredRedisConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("Redis")
        ?? configuration["Platform:Redis:ConnectionString"]
        ?? configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException(
            "Missing Redis connection string. Configure ConnectionStrings:Redis or Platform:Redis:ConnectionString.");

    private static bool IsProvider(string value, string provider) =>
        string.Equals(value, provider, StringComparison.OrdinalIgnoreCase);
}
