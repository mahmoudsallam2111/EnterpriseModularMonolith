using BuildingBlocks.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository
    : EfRepository<OrdersDbContext, Order, OrderId>, IOrderRepository
{
    public OrderRepository(OrdersDbContext context) : base(context) { }

    public Task<Order?> GetWithLinesAsync(OrderId id, CancellationToken cancellationToken = default) =>
        Context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
}
