using BuildingBlocks.Application.Persistence;
using BuildingBlocks.SharedKernel;
using BuildingBlocks.SharedKernel.DependencyInjection;
using Orders.Application.Dtos;
using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public interface IOrderQuery : IQueryBuilder<Order>, ITransientDependency
{
    Task<OrderDetailsDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<PagedList<OrderDetailsDto>> ListForCustomerAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetOpenOrderIdsForCustomerAsync(Guid customerId, CancellationToken cancellationToken);
}
