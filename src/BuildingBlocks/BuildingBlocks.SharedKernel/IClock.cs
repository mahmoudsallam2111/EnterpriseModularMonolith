namespace BuildingBlocks.SharedKernel;

/// <summary>
/// Abstraction over the system clock so domain and application logic stays deterministic and testable.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
