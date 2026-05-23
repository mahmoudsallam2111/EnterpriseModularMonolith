namespace BuildingBlocks.Infrastructure.Seeding;

/// <summary>
/// Module-owned seed routine. The host runs all registered seeders at startup,
/// in the order returned by <see cref="Order"/>, after migrations have been applied.
/// </summary>
public interface IDataSeeder
{
    int Order => 0;
    Task SeedAsync(CancellationToken cancellationToken);
}
