using Customers.Contracts;
using Microsoft.EntityFrameworkCore;
using Customers.Domain.Customers;

namespace Customers.Infrastructure.PublicApi;

/// <summary>
/// Default implementation of the public API surface. This is what other modules
/// (Orders, etc.) consume when they need read-only data about a customer. Returns
/// flat DTOs only — no Customer aggregate ever leaves this module.
/// </summary>
internal sealed class CustomersApi : ICustomersApi
{
    private readonly Persistence.CustomersDbContext _context;
    public CustomersApi(Persistence.CustomersDbContext context) => _context = context;

    public Task<CustomerSummaryDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        _context.Customers
            .AsNoTracking()
            .Where(c => c.Id.Value == customerId)
            .Select(c => new CustomerSummaryDto(
                c.Id.Value,
                c.Name.First + " " + c.Name.Last,
                c.Email.Value,
                c.Status == CustomerStatus.Active))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> CustomerExistsAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        _context.Customers.AsNoTracking().AnyAsync(c => c.Id.Value == customerId, cancellationToken);

    public Task<bool> IsCustomerActiveAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        _context.Customers
            .AsNoTracking()
            .AnyAsync(c => c.Id.Value == customerId && c.Status == CustomerStatus.Active, cancellationToken);
}
