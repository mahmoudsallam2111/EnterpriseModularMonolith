using BuildingBlocks.Domain.Persistence;

namespace Customers.Domain.Customers;

/// <summary>
/// Domain-defined repository interface. The implementation lives in Customers.Infrastructure,
/// but the abstraction belongs to the domain — that's the dependency inversion in action.
/// </summary>
public interface ICustomerRepository : IRepository<Customer, CustomerId>
{
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
}
