using BuildingBlocks.Domain;
using FluentAssertions;
using Orders.Domain.Orders;
using Orders.Domain.Orders.Events;
using Xunit;

namespace Orders.UnitTests.Domain;

public class OrderTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public void Draft_should_start_with_no_lines()
    {
        var order = Order.Draft(CustomerId, "USD");
        order.Lines.Should().BeEmpty();
        order.Status.Should().Be(OrderStatus.Draft);
    }

    [Fact]
    public void Cannot_place_order_with_no_lines()
    {
        var order = Order.Draft(CustomerId, "USD");
        var act = () => order.Place(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Adding_existing_sku_should_merge_quantity()
    {
        var order = Order.Draft(CustomerId, "USD");
        order.AddLine("SKU-1", "Widget", 2, 10m);
        order.AddLine("SKU-1", "Widget", 3, 10m);

        order.Lines.Should().HaveCount(1);
        order.Lines.Single().Quantity.Should().Be(5);
    }

    [Fact]
    public void Place_should_total_lines_and_raise_event()
    {
        var order = Order.Draft(CustomerId, "USD");
        order.AddLine("SKU-1", "Widget", 2, 10m);
        order.AddLine("SKU-2", "Gadget", 1, 25m);

        order.Place(DateTimeOffset.UtcNow);

        order.Status.Should().Be(OrderStatus.Placed);
        order.Total.Amount.Should().Be(45m);
        order.DomainEvents.Should().Contain(e => e is OrderPlacedDomainEvent);
    }

    [Fact]
    public void Currency_mismatch_should_throw()
    {
        var a = Money.Of(10m, "USD");
        var b = Money.Of(5m, "EUR");
        var act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Completed_order_cannot_be_cancelled()
    {
        var order = Order.Draft(CustomerId, "USD");
        order.AddLine("SKU-1", "Widget", 1, 10m);
        order.Place(DateTimeOffset.UtcNow);
        order.MarkPaid();
        order.MarkShipped();
        order.Complete(DateTimeOffset.UtcNow);

        var act = () => order.Cancel("changed my mind", DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleValidationException>();
    }
}
