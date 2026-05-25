using BuildingBlocks.Application.Persistence;
using BuildingBlocks.SharedKernel.DependencyInjection;
using Customers.Domain.Customers;

namespace Customers.Application.Customers;

public interface ICustomerQuery : IQueryBuilder<Customer>, ITransientDependency
{
}
