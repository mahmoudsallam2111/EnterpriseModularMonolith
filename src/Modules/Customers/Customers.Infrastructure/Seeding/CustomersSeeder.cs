using BuildingBlocks.Infrastructure.Seeding;
using Customers.Domain.Customers;
using Customers.Infrastructure.Persistence;

namespace Customers.Infrastructure.Seeding;

/// <summary>
/// Inserts a handful of demo customers on first run. Idempotent — re-running won't duplicate.
/// </summary>
public sealed class CustomersSeeder : IDataSeeder
{
    private readonly CustomersDbContext _context;
    public CustomersSeeder(CustomersDbContext context) => _context = context;

    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_context.Customers.Any()) return;

        var c1 = Customer.Register(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"));
        var c2 = Customer.Register(
            PersonName.Create("Grace", "Hopper"),
            Email.Create("grace@example.com"));
        var c3 = Customer.Register(
            PersonName.Create("Linus", "Torvalds"),
            Email.Create("linus@example.com"));

        await _context.Customers.AddRangeAsync(new[] { c1, c2, c3 }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
