using BuildingBlocks.Infrastructure.Persistence.Repositories;
using BuildingBlocks.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Dtos;
using Orders.Application.Orders;
using Orders.Application.Orders.Queries.GetOrderById;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence.Queries;

internal sealed class OrderQuery : EfQueryBuilder<OrdersDbContext, Order>, IOrderQuery
{
    private readonly OrdersDbContext _context;

    public OrderQuery(OrdersDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<OrderDetailsDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        _context.Orders
            .AsNoTracking()
            .Where(o => o.Id.Value == orderId)
            .Select(o => new OrderDetailsDto(
                o.Id.Value,
                o.CustomerId,
                o.Status.ToString(),
                o.Currency,
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.PlacedAtUtc,
                o.Lines.Select(l => new OrderLineDto(
                    l.Id.Value,
                    l.ProductSku,
                    l.ProductName,
                    l.Quantity,
                    l.UnitPrice.Amount,
                    l.UnitPrice.Amount * l.Quantity)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedList<OrderDetailsDto>> ListForCustomerAsync(Guid customerId, PageRequest page, CancellationToken cancellationToken)
    {
        var baseQuery = _context.Orders.AsNoTracking().Where(o => o.CustomerId == customerId);
        var total = await baseQuery.LongCountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(o => o.PlacedAtUtc)
            .Skip(page.Skip).Take(page.Take)
            .Select(o => new OrderDetailsDto(
                o.Id.Value,
                o.CustomerId,
                o.Status.ToString(),
                o.Currency,
                o.Lines.Sum(l => l.UnitPrice.Amount * l.Quantity),
                o.PlacedAtUtc,
                o.Lines.Select(l => new OrderLineDto(
                    l.Id.Value, l.ProductSku, l.ProductName, l.Quantity,
                    l.UnitPrice.Amount, l.UnitPrice.Amount * l.Quantity)).ToList()))
            .ToListAsync(cancellationToken);
        return new PagedList<OrderDetailsDto>(items, page.Page, page.Take, total);
    }

    public async Task<IReadOnlyList<Guid>> GetOpenOrderIdsForCustomerAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Completed)
            .Select(o => o.Id.Value)
            .ToListAsync(cancellationToken);
}
