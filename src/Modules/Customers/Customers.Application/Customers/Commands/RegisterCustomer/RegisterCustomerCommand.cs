using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Cqrs;

namespace Customers.Application.Customers.Commands.RegisterCustomer;

/// <summary>
/// Registers a new customer. Returns the new customer id on success.
/// </summary>
[RequiresPermission(CustomerPermissions.Manage)]
public sealed record RegisterCustomerCommand(
    string FirstName,
    string LastName,
    string Email) : ICommand<Guid>;
