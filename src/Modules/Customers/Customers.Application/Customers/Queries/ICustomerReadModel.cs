using BuildingBlocks.SharedKernel;
using Customers.Application.Customers.Queries.GetCustomer;

namespace Customers.Application.Customers.Queries;

/// <summary>
/// Read-side abstraction for projecting Customer data. Implemented over the same EF
/// model in the template — in a CQRS-with-separate-store deployment this would point
/// at a denormalised read store (Elastic, Redis, separate DB).
/// </summary>
public interface ICustomerReadModel
{
    Task<CustomerDetailsDto?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<PagedList<CustomerDetailsDto>> ListAsync(
        string? search, string? status, PageRequest page, CancellationToken cancellationToken);
}
