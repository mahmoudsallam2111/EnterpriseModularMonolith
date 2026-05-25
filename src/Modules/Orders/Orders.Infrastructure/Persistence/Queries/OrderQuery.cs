using BuildingBlocks.Infrastructure.Persistence.Repositories;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Queries;

internal sealed class OrderQuery : EfQueryBuilder<OrdersDbContext, Order>, IOrderQuery
{
    public OrderQuery(OrdersDbContext context) : base(context)
    {
    }
}
