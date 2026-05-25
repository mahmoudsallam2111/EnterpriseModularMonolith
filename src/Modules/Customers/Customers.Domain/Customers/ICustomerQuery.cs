using BuildingBlocks.Application.Persistence;
using BuildingBlocks.SharedKernel.DependencyInjection;

namespace Customers.Domain.Customers;

public interface ICustomerQuery : IQueryBuilder<Customer>, ITransientDependency
{
}
