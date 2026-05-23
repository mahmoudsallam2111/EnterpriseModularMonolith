namespace Customers.Contracts;

/// <summary>
/// Public surface of the Customers module. Other modules (Orders, etc.) consume
/// customer data through THIS interface — never via Customers' domain types or
/// DbContext. This contract is the only Customers project that other modules reference.
/// </summary>
public interface ICustomersApi
{
    Task<CustomerSummaryDto?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> CustomerExistsAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> IsCustomerActiveAsync(Guid customerId, CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO used by other modules. Intentionally narrow — only stable, public-facing fields.
/// </summary>
public sealed record CustomerSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    bool IsActive);
