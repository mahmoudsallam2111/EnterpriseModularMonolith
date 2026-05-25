using BuildingBlocks.Domain;
using Customers.Domain.Customers.Events;
using Customers.Domain.Customers.Rules;
using Customers.Domain.Customers.ValueObjects;

namespace Customers.Domain.Customers;

/// <summary>
/// Customer aggregate root. Owns its email, name, addresses and lifecycle state.
/// All state changes go through the methods on this class — there are no public setters.
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>, IAuditableEntity, ISoftDeletable
{
    private readonly List<Address> _addresses = [];

    public PersonName Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public CustomerStatus Status { get; private set; }
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    // Auditing fields (stamped by interceptor)
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // EF Core
    private Customer() { }

    private Customer(CustomerId id, PersonName name, Email email)
        : base(id)
    {
        Name = name;
        Email = email;
        Status = CustomerStatus.Active;
    }

    public static Customer Register(PersonName name, Email email)
    {
        CheckRule(new EmailMustBeProvided(email));
        CheckRule(new NameMustBeProvided(name));

        var customer = new Customer(CustomerId.New(), name, email);
        customer.RaiseDomainEvent(new CustomerRegisteredDomainEvent(customer.Id, name.Full, email.Value));
        return customer;
    }

    public void ChangeEmail(Email newEmail)
    {
        if (Email == newEmail) return;
        CheckRule(new CustomerMustBeActive(Status));
        var old = Email;
        Email = newEmail;
        RaiseDomainEvent(new CustomerEmailChangedDomainEvent(Id, old.Value, newEmail.Value));
    }

    public void Rename(PersonName newName)
    {
        if (Name == newName) return;
        CheckRule(new CustomerMustBeActive(Status));
        Name = newName;
    }

    public void AddAddress(Address address)
    {
        CheckRule(new CustomerMustBeActive(Status));
        CheckRule(new CustomerCannotHaveDuplicateAddress(_addresses, address));
        _addresses.Add(address);
    }

    public void RemoveAddress(Address address)
    {
        CheckRule(new CustomerMustBeActive(Status));
        _addresses.Remove(address);
    }

    public void Suspend(string reason)
    {
        if (Status == CustomerStatus.Suspended) return;
        CheckRule(new CustomerMustBeActive(Status));
        Status = CustomerStatus.Suspended;
        RaiseDomainEvent(new CustomerSuspendedDomainEvent(Id, reason));
    }

    public void Reactivate()
    {
        if (Status == CustomerStatus.Active) return;
        if (Status == CustomerStatus.Deactivated)
            throw new InvalidOperationException("Deactivated customers cannot be reactivated.");
        Status = CustomerStatus.Active;
        RaiseDomainEvent(new CustomerReactivatedDomainEvent(Id));
    }

    public void Deactivate(string reason)
    {
        if (Status == CustomerStatus.Deactivated) return;
        Status = CustomerStatus.Deactivated;
        RaiseDomainEvent(new CustomerDeactivatedDomainEvent(Id, reason));
    }
}
