using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;
using Customers.Application.Dtos;

namespace Customers.Application.Customers.Queries.GetCustomer;

internal sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDetailsDto>
{
    private readonly ICustomerQuery _query;
    public GetCustomerByIdQueryHandler(ICustomerQuery query) => _query = query;

    public async Task<Result<CustomerDetailsDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _query.GetByIdAsync(request.CustomerId, cancellationToken);
        if (dto is null)
            return Error.NotFound("Customers.NotFound", $"Customer {request.CustomerId} not found.");
        return dto;
    }
}