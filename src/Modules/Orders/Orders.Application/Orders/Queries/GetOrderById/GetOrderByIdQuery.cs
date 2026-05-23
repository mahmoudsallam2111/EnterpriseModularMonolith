using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;

namespace Orders.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDetailsDto>;

public sealed record OrderDetailsDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    string Currency,
    decimal Total,
    DateTimeOffset? PlacedAtUtc,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record OrderLineDto(Guid Id, string Sku, string Name, int Quantity, decimal UnitPrice, decimal LineTotal);

internal sealed class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDetailsDto>
{
    private readonly IOrderReadModel _readModel;
    public GetOrderByIdQueryHandler(IOrderReadModel readModel) => _readModel = readModel;

    public async Task<Result<OrderDetailsDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _readModel.GetByIdAsync(request.OrderId, cancellationToken);
        return dto is null
            ? Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found.")
            : dto;
    }
}

public interface IOrderReadModel
{
    Task<OrderDetailsDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<PagedList<OrderDetailsDto>> ListForCustomerAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken);
}
