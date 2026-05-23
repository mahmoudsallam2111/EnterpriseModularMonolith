namespace Orders.Contracts;

/// <summary>
/// Public surface of the Orders module — narrow, stable DTOs only.
/// </summary>
public interface IOrdersApi
{
    Task<OrderSummaryDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}

public sealed record OrderSummaryDto(
    Guid Id,
    Guid CustomerId,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTimeOffset PlacedAtUtc);
