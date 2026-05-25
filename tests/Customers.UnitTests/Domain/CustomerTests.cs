using BuildingBlocks.Domain;
using Customers.Domain.Customers;
using Customers.Domain.Customers.Events;
using Customers.Domain.Customers.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Customers.UnitTests.Domain;

public class CustomerTests
{
    [Fact]
    public void Register_should_create_active_customer_and_raise_event()
    {
        var customer = Customer.Register(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"));

        customer.Status.Should().Be(CustomerStatus.Active);
        customer.DomainEvents.Should().ContainSingle(e => e is CustomerRegisteredDomainEvent);
    }

    [Fact]
    public void Email_should_normalise_to_lower_case()
    {
        var email = Email.Create("Ada@Example.COM");
        email.Value.Should().Be("ada@example.com");
    }

    [Fact]
    public void ChangeEmail_should_be_idempotent_when_email_unchanged()
    {
        var customer = Customer.Register(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"));
        customer.ClearDomainEvents();

        customer.ChangeEmail(Email.Create("ada@example.com"));

        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deactivated_customer_cannot_be_edited()
    {
        var customer = Customer.Register(
            PersonName.Create("Ada", "Lovelace"),
            Email.Create("ada@example.com"));
        customer.Deactivate("test");

        var act = () => customer.ChangeEmail(Email.Create("new@example.com"));

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Email_must_have_an_at_sign()
    {
        var act = () => Email.Create("not-an-email");
        act.Should().Throw<ArgumentException>();
    }
}
