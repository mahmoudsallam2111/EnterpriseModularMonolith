using BuildingBlocks.Application.Cqrs;
using BuildingBlocks.SharedKernel;

namespace Customers.Application.Customers.Queries.GetCustomer;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDetailsDto>;

public sealed record CustomerDetailsDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Status,
    DateTimeOffset CreatedAt);

internal sealed class GetCustomerByIdQueryHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDetailsDto>
{
    private readonly ICustomerReadModel _readModel;
    public GetCustomerByIdQueryHandler(ICustomerReadModel readModel) => _readModel = readModel;

    public async Task<Result<CustomerDetailsDto>> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _readModel.GetByIdAsync(request.CustomerId, cancellationToken);
        if (dto is null)
            return Error.NotFound("Customers.NotFound", $"Customer {request.CustomerId} not found.");
        return dto;
    }
}

public interface ICustomerReadModel
{
    Task<CustomerDetailsDto?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken);
    Task<PagedList<CustomerDetailsDto>> ListAsync(
        string? search,
        string? status,
        PageRequest page,
        CancellationToken cancellationToken);
}
