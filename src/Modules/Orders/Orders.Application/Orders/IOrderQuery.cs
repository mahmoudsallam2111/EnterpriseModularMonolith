using BuildingBlocks.Application.Persistence;
using BuildingBlocks.SharedKernel.DependencyInjection;
using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public interface IOrderQuery : IQueryBuilder<Order>, ITransientDependency
{
}
