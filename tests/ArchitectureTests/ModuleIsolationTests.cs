using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace ArchitectureTests;

/// <summary>
/// Enforces the "modules don't touch each other's internals" rule. Modules may
/// communicate only via Contracts / IntegrationEvents / public APIs — referencing
/// another module's Domain/Application/Infrastructure assembly fails the build.
/// </summary>
public sealed class ModuleIsolationTests
{
    private const string CustomersDomain = "Customers.Domain";
    private const string CustomersApp = "Customers.Application";
    private const string CustomersInfra = "Customers.Infrastructure";

    private const string OrdersDomain = "Orders.Domain";
    private const string OrdersApp = "Orders.Application";
    private const string OrdersInfra = "Orders.Infrastructure";

    private const string UsersDomain = "Users.Domain";
    private const string UsersApp = "Users.Application";
    private const string UsersInfra = "Users.Infrastructure";

    [Theory]
    [InlineData(typeof(Customers.Domain.Customers.Customer), OrdersDomain, OrdersApp, OrdersInfra, UsersDomain, UsersApp, UsersInfra)]
    [InlineData(typeof(Orders.Domain.Orders.Order), CustomersDomain, CustomersApp, CustomersInfra, UsersDomain, UsersApp, UsersInfra)]
    [InlineData(typeof(Users.Domain.Users.User), CustomersDomain, CustomersApp, CustomersInfra, OrdersDomain, OrdersApp, OrdersInfra)]
    public void Domain_should_not_depend_on_any_other_module(Type marker, params string[] forbidden)
    {
        var assembly = marker.Assembly;
        var result = Types.InAssembly(assembly)
            .Should()
            .NotHaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain projects must be free of cross-module references. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Theory]
    [InlineData(typeof(Customers.Application.CustomerPermissions), OrdersDomain, OrdersInfra, UsersDomain, UsersInfra)]
    [InlineData(typeof(Orders.Application.OrderPermissions), CustomersDomain, CustomersInfra, UsersDomain, UsersInfra)]
    [InlineData(typeof(Users.Application.UserPermissions), CustomersDomain, CustomersInfra, OrdersDomain, OrdersInfra)]
    public void Application_can_consume_contracts_and_integration_events_but_not_other_modules_domain_or_infra(
        Type marker, params string[] forbidden)
    {
        var result = Types.InAssembly(marker.Assembly)
            .Should()
            .NotHaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application projects may only reference their own module's domain, the BuildingBlocks, " +
            "and OTHER modules' Contracts/IntegrationEvents. Failing types: " +
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Domain_should_not_reference_infrastructure_at_all()
    {
        var domains = new[]
        {
            typeof(Customers.Domain.Customers.Customer).Assembly,
            typeof(Orders.Domain.Orders.Order).Assembly,
            typeof(Users.Domain.Users.User).Assembly,
            typeof(BuildingBlocks.Domain.Entity<>).Assembly,
        };

        foreach (var asm in domains)
        {
            var result = Types.InAssembly(asm)
                .Should()
                .NotHaveDependencyOnAny(
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.AspNetCore",
                    "Npgsql",
                    "Serilog")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{asm.GetName().Name} must not depend on infrastructure libraries. Failing: " +
                string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
        }
    }

    [Fact]
    public void Application_should_not_reference_aspnet_or_efcore()
    {
        var assemblies = new[]
        {
            typeof(Customers.Application.CustomerPermissions).Assembly,
            typeof(Orders.Application.OrderPermissions).Assembly,
            typeof(Users.Application.UserPermissions).Assembly,
            typeof(BuildingBlocks.Application.Cqrs.ICommand).Assembly,
        };

        foreach (var asm in assemblies)
        {
            var result = Types.InAssembly(asm)
                .Should()
                .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(
                $"{asm.GetName().Name} must remain framework-light. Failing: " +
                string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
        }
    }

    [Fact]
    public void Contracts_assemblies_must_be_dependency_free()
    {
        var contracts = new[]
        {
            typeof(Customers.Contracts.ICustomersApi).Assembly,
            typeof(Orders.Contracts.IOrdersApi).Assembly,
            typeof(Users.Contracts.IUsersApi).Assembly,
        };

        foreach (var asm in contracts)
        {
            var referenced = asm.GetReferencedAssemblies();
            referenced.Should().OnlyContain(
                a => a.Name == "System.Runtime" ||
                     a.Name == "System.Private.CoreLib" ||
                     a.Name!.StartsWith("System.") ||
                     a.Name == "netstandard" ||
                     a.Name == "mscorlib",
                $"{asm.GetName().Name} must remain a pure contract — no dependencies on infrastructure or domain.");
        }
    }
}
