using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingBlocks.Infrastructure.Caching;

/// <summary>
/// Default in-process implementation of <see cref="ICacheService"/> backed by IMemoryCache.
/// Swap for a Redis-backed implementation in production by re-registering the interface.
/// </summary>
public sealed class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public InMemoryCacheService(IMemoryCache cache) => _cache = cache;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<T>(key, out var value))
            return Task.FromResult<T?>(value);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var entryOptions = new MemoryCacheEntryOptions();
        if (ttl is { } t) entryOptions.AbsoluteExpirationRelativeToNow = t;
        _cache.Set(key, value, entryOptions);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrAddAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue<T>(key, out var existing) && existing is not null)
            return existing;

        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);
        return value;
    }
}
