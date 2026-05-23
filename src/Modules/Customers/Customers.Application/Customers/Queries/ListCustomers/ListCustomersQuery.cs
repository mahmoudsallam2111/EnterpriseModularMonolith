using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using Customers.Application.Customers.Queries.GetCustomer;

namespace Customers.Application.Customers.Queries.ListCustomers;

public sealed record ListCustomersQuery(
    string? Search,
    string? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedList<CustomerDetailsDto>>;

internal sealed class ListCustomersQueryHandler : IQueryHandler<ListCustomersQuery, PagedList<CustomerDetailsDto>>
{
    private readonly ICustomerReadModel _readModel;
    public ListCustomersQueryHandler(ICustomerReadModel readModel) => _readModel = readModel;

    public async Task<Result<PagedList<CustomerDetailsDto>>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var page = await _readModel.ListAsync(
            request.Search, request.Status,
            new PageRequest(request.Page, request.PageSize),
            cancellationToken);
        return page;
    }
}
