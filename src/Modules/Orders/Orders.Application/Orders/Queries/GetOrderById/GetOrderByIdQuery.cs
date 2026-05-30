using BuildingBlocks.Application.Cqrs;
using Orders.Application.Dtos;

namespace Orders.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailsDto>;
