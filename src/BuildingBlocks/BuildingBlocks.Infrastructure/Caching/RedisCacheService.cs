using System.Text.Json;
using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisCacheOptions _options;

    public RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisCacheOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _redis.GetDatabase().StringGetAsync(BuildKey(key));
        cancellationToken.ThrowIfCancellationRequested();

        return value.HasValue
            ? JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions)
            : default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(value, JsonOptions);
        await _redis.GetDatabase().StringSetAsync(BuildKey(key), payload, ttl);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _redis.GetDatabase().KeyDeleteAsync(BuildKey(key));
    }

    public async Task<T> GetOrAddAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null)
            return existing;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);
        return value;
    }

    private RedisKey BuildKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return $"{_options.KeyPrefix}{key}";
    }
}
