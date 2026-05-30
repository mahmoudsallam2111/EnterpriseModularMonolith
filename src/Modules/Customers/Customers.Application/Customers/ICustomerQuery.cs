using BuildingBlocks.Application.Persistence;
using BuildingBlocks.SharedKernel;
using BuildingBlocks.SharedKernel.DependencyInjection;
using Customers.Application.Dtos;
using Customers.Domain.Customers;

namespace Customers.Application.Customers;

public interface ICustomerQuery : IQueryBuilder<Customer>, ITransientDependency
{
    Task<CustomerDetailsDto?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<PagedList<CustomerDetailsDto>> ListAsync(
        string? search,
        string? status,
        PageRequest page,
        CancellationToken cancellationToken);
}
