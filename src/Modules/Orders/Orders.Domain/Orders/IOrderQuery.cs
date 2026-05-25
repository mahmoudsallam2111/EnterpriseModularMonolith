using BuildingBlocks.Application.Persistence;
using BuildingBlocks.SharedKernel.DependencyInjection;

namespace Orders.Domain.Orders;

public interface IOrderQuery : IQueryBuilder<Order>, ITransientDependency
{
}
