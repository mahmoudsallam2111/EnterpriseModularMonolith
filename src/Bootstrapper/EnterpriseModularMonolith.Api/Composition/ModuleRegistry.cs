using BuildingBlocks.Application.Modules;
using Customers.Infrastructure;
using Orders.Infrastructure;
using Inventories.Infrastructure;

namespace EnterpriseModularMonolith.Api.Composition;

internal static class ModuleRegistry
{
    public static IReadOnlyList<IModule> All { get; } = new IModule[]
    {
        new CustomersModule(),
        new OrdersModule(),
        new InventoriesModule(),
    };
}
