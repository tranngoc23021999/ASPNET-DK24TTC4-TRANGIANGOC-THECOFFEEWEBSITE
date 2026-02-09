namespace CoffeSolution.Constants;

/// <summary>
/// Menu codes for permission system.
/// Using const string instead of enum because:
/// - Can be used directly in [Permission] attributes
/// - Matches directly with database string values
/// - No conversion needed for comparisons
/// </summary>
public static class MenuCode
{
    public const string Dashboard = "DASHBOARD";
    public const string Store = "STORE";
    public const string Product = "PRODUCT";
    public const string Order = "ORDER";
    public const string POS = "POS";
    public const string Warehouse = "WAREHOUSE";
    public const string Supplier = "SUPPLIER";
    public const string Customer = "CUSTOMER";
    public const string Employee = "EMPLOYEE";
    public const string User = "USER";
    public const string Role = "ROLE";
    public const string Report = "REPORT";
    public const string Setting = "SETTING";
}
