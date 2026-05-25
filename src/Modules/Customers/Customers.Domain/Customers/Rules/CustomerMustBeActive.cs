using BuildingBlocks.Domain;
using Customers.Domain.Customers.ValueObjects;

namespace Customers.Domain.Customers.Rules;

public sealed class CustomerMustBeActive : IBusinessRule
{
    private readonly CustomerStatus _status;
    public CustomerMustBeActive(CustomerStatus status) => _status = status;

    public string Code => "Customers.MustBeActive";
    public string Message => $"Operation not allowed: customer is {_status}.";
    public bool IsBroken() => _status != CustomerStatus.Active;
}

public sealed class EmailMustBeProvided : IBusinessRule
{
    private readonly Email? _email;
    public EmailMustBeProvided(Email? email) => _email = email;
    public string Code => "Customers.EmailRequired";
    public string Message => "Customer email must be provided.";
    public bool IsBroken() => _email is null;
}

public sealed class NameMustBeProvided : IBusinessRule
{
    private readonly PersonName? _name;
    public NameMustBeProvided(PersonName? name) => _name = name;
    public string Code => "Customers.NameRequired";
    public string Message => "Customer name must be provided.";
    public bool IsBroken() => _name is null;
}

public sealed class CustomerCannotHaveDuplicateAddress : IBusinessRule
{
    private readonly IEnumerable<Address> _existing;
    private readonly Address _candidate;
    public CustomerCannotHaveDuplicateAddress(IEnumerable<Address> existing, Address candidate)
    {
        _existing = existing; _candidate = candidate;
    }
    public string Code => "Customers.DuplicateAddress";
    public string Message => "Customer already has this address.";
    public bool IsBroken() => _existing.Any(a => a == _candidate);
}
