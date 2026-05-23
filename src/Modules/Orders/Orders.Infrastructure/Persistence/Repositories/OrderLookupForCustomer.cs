using Microsoft.EntityFrameworkCore;
using Orders.Application.Orders.EventHandlers;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Repositories;

internal sealed class OrderLookupForCustomer : IOrderLookupForCustomer
{
    private readonly OrdersDbContext _context;
    public OrderLookupForCustomer(OrdersDbContext context) => _context = context;

    public async Task<IReadOnlyList<Guid>> GetOpenOrdersForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Completed)
            .Select(o => o.Id.Value)
            .ToListAsync(cancellationToken);
}
