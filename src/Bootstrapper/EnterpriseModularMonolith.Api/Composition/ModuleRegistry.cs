using BuildingBlocks.Application.Modules;
using Customers.Infrastructure;
using Orders.Infrastructure;
using Users.Infrastructure;

namespace EnterpriseModularMonolith.Api.Composition;

/// <summary>
/// The set of modules composed by the host. Adding a new module = adding one line here.
/// The rest of the boot pipeline iterates the list and asks each module to register itself.
/// </summary>
internal static class ModuleRegistry
{
    public static IReadOnlyList<IModule> All { get; } = new IModule[]
    {
        new UsersModule(),       // Users first — others depend on it for permissions
        new CustomersModule(),
        new OrdersModule(),
    };
}
