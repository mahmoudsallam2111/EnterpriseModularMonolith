using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using Orders.Application.Dtos;

namespace Orders.Application.Orders.Queries.GetOrderById;

internal sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDetailsDto>
{
    private readonly IOrderQuery _query;
    public GetOrderByIdQueryHandler(IOrderQuery query) => _query = query;

    public async Task<Result<OrderDetailsDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _query.GetByIdAsync(request.OrderId, cancellationToken);
        return dto is null
            ? Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found.")
            : dto;
    }
}