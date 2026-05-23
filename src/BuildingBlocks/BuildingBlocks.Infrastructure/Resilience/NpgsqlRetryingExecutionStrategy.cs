// Intentionally minimal — Npgsql's EnableRetryOnFailure() in module DI gives us retries.
// Kept here as a documented extension point so teams can centralise resiliency policy later.
namespace BuildingBlocks.Infrastructure.Resilience;

public static class ResilienceDefaults
{
    public const int MaxRetryCount = 3;
    public static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(5);
}
