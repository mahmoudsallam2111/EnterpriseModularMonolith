using BuildingBlocks.Infrastructure.Seeding;
using Customers.Infrastructure.Persistence;
using Customers.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Infrastructure.Persistence;

namespace EnterpriseModularMonolith.Api.Composition;

internal static class MigrationsAndSeed
{
    /// <summary>
    /// Applies pending EF migrations for every module's DbContext, then runs every
    /// registered <see cref="IDataSeeder"/>.
    /// </summary>
    public static async Task MigrateAndSeedAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Bootstrap");

        // Register seeders here (host owns wiring — modules just provide the classes).
        var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        var ordersDb = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        foreach (var db in new DbContext[] { customersDb, ordersDb })
        {
            logger.LogInformation("Applying migrations for {Context}", db.GetType().Name);
            await db.Database.MigrateAsync(cancellationToken);
        }

        var seeders = new IDataSeeder[]
        {
            new CustomersSeeder(customersDb),
        }.OrderBy(s => s.Order);

        foreach (var seeder in seeders)
        {
            logger.LogInformation("Running seeder {Seeder}", seeder.GetType().Name);
            await seeder.SeedAsync(cancellationToken);
        }
    }
}
