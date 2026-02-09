namespace CoffeSolution.Constants;

/// <summary>
/// Action codes for permission system.
/// Using const string instead of enum because:
/// - Can be used directly in [Permission] attributes
/// - Matches directly with database string values
/// - No conversion needed for comparisons
/// </summary>
public static class ActionCode
{
    public const string View = "VIEW";
    public const string Create = "CREATE";
    public const string Edit = "EDIT";
    public const string Delete = "DELETE";
    public const string Export = "EXPORT";
    public const string Import = "IMPORT";
    public const string Print = "PRINT";
    public const string Approve = "APPROVE";
    public const string Cancel = "CANCEL";
}
