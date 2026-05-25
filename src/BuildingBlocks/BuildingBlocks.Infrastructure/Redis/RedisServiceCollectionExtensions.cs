using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Locking;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Infrastructure.Locking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Redis;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisConnectionMultiplexer(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        return services;
    }

    public static IServiceCollection AddRedisCacheService(
        this IServiceCollection services,
        string connectionString,
        string keyPrefix)
    {
        services.AddRedisConnectionMultiplexer(connectionString);
        services.Configure<RedisCacheOptions>(options => options.KeyPrefix = keyPrefix);
        services.AddScoped<ICacheService, RedisCacheService>();
        return services;
    }

    public static IServiceCollection AddRedisDistributedLock(
        this IServiceCollection services,
        string connectionString,
        string keyPrefix)
    {
        services.AddRedisConnectionMultiplexer(connectionString);
        services.Configure<RedisDistributedLockOptions>(options => options.KeyPrefix = keyPrefix);
        services.AddSingleton<IDistributedLock, RedisDistributedLock>();
        return services;
    }
}
