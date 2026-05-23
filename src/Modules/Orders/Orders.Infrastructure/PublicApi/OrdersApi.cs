using Microsoft.EntityFrameworkCore;
using Orders.Contracts;

namespace Orders.Infrastructure.PublicApi;

internal sealed class OrdersApi : IOrdersApi
{
    private readonly Persistence.OrdersDbContext _context;
    public OrdersApi(Persistence.OrdersDbContext context) => _context = context;

    public Task<OrderSummaryDto?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        _context.Orders
            .AsNoTracking()
            .Where(o => o.Id.Value == orderId)
            .Select(o => new OrderSummaryDto(
                o.Id.Value, o.CustomerId,
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.Currency, o.Status.ToString(),
                o.PlacedAtUtc ?? o.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default) =>
        await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.PlacedAtUtc)
            .Select(o => new OrderSummaryDto(
                o.Id.Value, o.CustomerId,
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.Currency, o.Status.ToString(),
                o.PlacedAtUtc ?? o.CreatedAt))
            .ToListAsync(cancellationToken);
}
