namespace EmmModule.Application;

/// <summary>
/// Permission identifiers owned by the EmmModule module. The Users module is the
/// grant authority — these strings are what it issues and verifies.
/// </summary>
public static class EmmModulePermissions
{
    public const string View   = "emmmodule.view";
    public const string Manage = "emmmodule.manage";
}
