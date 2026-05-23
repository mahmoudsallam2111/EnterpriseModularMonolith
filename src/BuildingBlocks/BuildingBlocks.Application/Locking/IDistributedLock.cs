namespace BuildingBlocks.Application.Locking;

/// <summary>
/// Distributed mutual exclusion. Default implementation is in-process; a Redis or
/// SQL implementation can be plugged in for a horizontally scaled deployment.
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Acquires a named lock. Returns null if the lock cannot be acquired within <paramref name="wait"/>.
    /// </summary>
    Task<IAsyncDisposable?> AcquireAsync(string resource, TimeSpan wait, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
}
