namespace CoffeSolution.Models.Entities;

/// <summary>
/// Bảng junction: Role ↔ Menu ↔ Action
/// Gắn quyền cụ thể cho Role trên từng Menu
/// </summary>
public class RoleMenuPermission
{
    public int Id { get; set; }

    // Role nào
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Menu nào
    public int MenuId { get; set; }
    public Menu Menu { get; set; } = null!;

    // Action nào (VIEW, CREATE, EDIT, DELETE...)
    public int MenuActionId { get; set; }
    public MenuAction MenuAction { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.Now;
}
