using BuildingBlocks.Domain.Persistence;

namespace Orders.Domain.Orders;

public interface IOrderRepository : IRepository<Order, OrderId>
{
    /// <summary>Load with lines included — call this when you intend to mutate the order.</summary>
    Task<Order?> GetWithLinesAsync(OrderId id, CancellationToken cancellationToken = default);
}
