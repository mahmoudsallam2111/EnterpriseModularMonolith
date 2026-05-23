using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ArchitectureTests;

public sealed class CleanArchitectureTests
{
    [Fact]
    public void Aggregate_roots_should_be_sealed()
    {
        var aggregateRoots = new[]
        {
            typeof(Customers.Domain.Customers.Customer),
            typeof(Orders.Domain.Orders.Order),
        };

        foreach (var t in aggregateRoots)
            t.IsSealed.Should().BeTrue($"{t.Name} is an aggregate root and should be sealed.");
    }

    [Fact]
    public void Domain_events_should_be_records_with_no_setters()
    {
        var assemblies = new[]
        {
            typeof(Customers.Domain.Customers.Customer).Assembly,
            typeof(Orders.Domain.Orders.Order).Assembly,
        };

        foreach (var asm in assemblies)
        {
            var result = Types.InAssembly(asm)
                .That()
                .ImplementInterface(typeof(BuildingBlocks.Domain.IDomainEvent))
                .Should()
                .BeSealed()
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Domain events in {asm.GetName().Name} must be sealed records. Failing: " +
                string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
        }
    }

    [Fact]
    public void Command_handlers_should_be_internal()
    {
        var assemblies = new[]
        {
            typeof(Customers.Application.CustomerPermissions).Assembly,
            typeof(Orders.Application.OrderPermissions).Assembly,
        };

        foreach (var asm in assemblies)
        {
            var result = Types.InAssembly(asm)
                .That()
                .HaveNameEndingWith("CommandHandler")
                .Should()
                .NotBePublic()
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"Handlers in {asm.GetName().Name} must be internal so they aren't part of the module's public surface. Failing: " +
                string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
        }
    }
}
