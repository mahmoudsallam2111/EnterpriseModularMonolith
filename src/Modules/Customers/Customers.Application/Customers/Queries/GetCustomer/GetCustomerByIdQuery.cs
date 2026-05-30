using BuildingBlocks.Application.Cqrs;
using Customers.Application.Dtos;

namespace Customers.Application.Customers.Queries.GetCustomer;

public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDetailsDto>;
