namespace CoffeSolution.Models.Entities;

/// <summary>
/// Menu hệ thống
/// - Menu có nhiều Action (View, Create, Edit, Delete...)
/// - Menu có thể có Parent (menu cha)
/// </summary>
public class Menu
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Code { get; set; } = null!;  // Mã menu: DASHBOARD, ORDER, PRODUCT...
    public string Url { get; set; } = null!;
    public string? Icon { get; set; }  // Font Awesome icon class
    public int Order { get; set; } = 0;  // Thứ tự hiển thị
    public bool IsActive { get; set; } = true;

    // Menu cha (nếu có)
    public int? ParentId { get; set; }
    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = new List<Menu>();

    // Menu có nhiều Action
    public ICollection<MenuAction> MenuActions { get; set; } = new List<MenuAction>();

    // Menu được gắn vào Role qua RoleMenuPermission
    public ICollection<RoleMenuPermission> RoleMenuPermissions { get; set; } = new List<RoleMenuPermission>();
}
