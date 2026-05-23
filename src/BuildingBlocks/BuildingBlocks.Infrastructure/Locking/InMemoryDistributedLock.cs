using System.Collections.Concurrent;
using BuildingBlocks.Application.Locking;

namespace BuildingBlocks.Infrastructure.Locking;

/// <summary>
/// In-process default implementation of <see cref="IDistributedLock"/>. Fine for a
/// single-process monolith. For multi-process deployments swap in a Redis (RedLock)
/// or SQL advisory-lock implementation behind the same interface.
/// </summary>
public sealed class InMemoryDistributedLock : IDistributedLock
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Semaphores = new();

    public async Task<IAsyncDisposable?> AcquireAsync(string resource, TimeSpan wait, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        var sem = Semaphores.GetOrAdd(resource, _ => new SemaphoreSlim(1, 1));
        if (!await sem.WaitAsync(wait, cancellationToken))
            return null;
        return new Release(sem);
    }

    private sealed class Release : IAsyncDisposable
    {
        private readonly SemaphoreSlim _sem;
        private bool _released;
        public Release(SemaphoreSlim sem) => _sem = sem;
        public ValueTask DisposeAsync()
        {
            if (_released) return ValueTask.CompletedTask;
            _released = true;
            _sem.Release();
            return ValueTask.CompletedTask;
        }
    }
}
