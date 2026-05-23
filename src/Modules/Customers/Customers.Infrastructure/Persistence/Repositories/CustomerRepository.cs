using BuildingBlocks.Infrastructure.Persistence.Repositories;
using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository
    : EfWriteRepository<CustomersDbContext, Customer, CustomerId>, ICustomerRepository
{
    public CustomerRepository(CustomersDbContext context) : base(context) { }

    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Context.Customers.AnyAsync(c => c.Email.Value == email.Value, cancellationToken);
}
