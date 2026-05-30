using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using Customers.Application.Dtos;

namespace Customers.Application.Customers.Queries.ListCustomers;

public sealed record ListCustomersQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<CustomerDetailsDto>>;

internal sealed class ListCustomersQueryHandler : IQueryHandler<ListCustomersQuery, PagedList<CustomerDetailsDto>>
{
    private readonly ICustomerQuery _query;
    public ListCustomersQueryHandler(ICustomerQuery query) => _query = query;

    public async Task<Result<PagedList<CustomerDetailsDto>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var page = await _query.ListAsync(
            request.Search, request.Status,
            new PageRequest(request.Page, request.PageSize),
            cancellationToken);
        return page;
    }
}
