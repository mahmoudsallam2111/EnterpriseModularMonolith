using BuildingBlocks.Infrastructure.Persistence.Repositories;
using Customers.Application.Customers.Queries;
using Customers.Domain.Customers;

namespace Customers.Infrastructure.Persistence.Queries;

internal sealed class CustomerQuery : EfQueryBuilder<CustomersDbContext, Customer>, ICustomerQuery
{
    public CustomerQuery(CustomersDbContext context) : base(context)
    {
    }
}
