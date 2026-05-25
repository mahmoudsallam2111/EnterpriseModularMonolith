using System.Diagnostics;
using BuildingBlocks.Application.Locking;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Locking;

public sealed class RedisDistributedLock : IDistributedLock
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private readonly IConnectionMultiplexer _redis;
    private readonly RedisDistributedLockOptions _options;

    public RedisDistributedLock(
        IConnectionMultiplexer redis,
        IOptions<RedisDistributedLockOptions> options)
    {
        _redis = redis;
        _options = options.Value;
    }

    public async Task<IAsyncDisposable?> AcquireAsync(
        string resource,
        TimeSpan wait,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        if (wait < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(wait), "Wait must not be negative.");
        if (leaseDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(leaseDuration), "Lease duration must be positive.");

        var database = _redis.GetDatabase();
        var key = BuildKey(resource);
        var token = Guid.NewGuid().ToString("N");
        var elapsed = Stopwatch.StartNew();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await database.LockTakeAsync(key, token, leaseDuration))
                return new RedisLockLease(database, key, token);

            if (wait == TimeSpan.Zero || elapsed.Elapsed >= wait)
                return null;

            var remaining = wait - elapsed.Elapsed;
            var delay = remaining < PollInterval ? remaining : PollInterval;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);
        }
    }

    private RedisKey BuildKey(string resource) => $"{_options.KeyPrefix}{resource}";

    private sealed class RedisLockLease : IAsyncDisposable
    {
        private readonly IDatabase _database;
        private readonly RedisKey _key;
        private readonly RedisValue _token;
        private bool _released;

        public RedisLockLease(IDatabase database, RedisKey key, RedisValue token)
        {
            _database = database;
            _key = key;
            _token = token;
        }

        public async ValueTask DisposeAsync()
        {
            if (_released)
                return;

            _released = true;
            await _database.LockReleaseAsync(_key, _token);
        }
    }
}
