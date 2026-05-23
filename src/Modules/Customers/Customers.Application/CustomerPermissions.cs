namespace Customers.Application;

/// <summary>
/// Permission identifiers owned by the Customers module. The Users module is the
/// permission authority — these strings are what it grants/revokes.
/// </summary>
public static class CustomerPermissions
{
    public const string View = "customers.view";
    public const string Manage = "customers.manage";
}
