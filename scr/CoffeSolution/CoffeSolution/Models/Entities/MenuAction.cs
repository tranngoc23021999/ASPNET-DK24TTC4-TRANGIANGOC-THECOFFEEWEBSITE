namespace CoffeSolution.Models.Entities;

/// <summary>
/// Action của Menu: View, Create, Edit, Delete, Export...
/// </summary>
public class MenuAction
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;  // Tên hiển thị: Xem, Thêm, Sửa, Xóa
    public string Code { get; set; } = null!;  // Mã: VIEW, CREATE, EDIT, DELETE, EXPORT
    public int Order { get; set; } = 0;

    // Thuộc Menu nào
    public int MenuId { get; set; }
    public Menu Menu { get; set; } = null!;

    // Action được gắn vào Role qua RoleMenuPermission
    public ICollection<RoleMenuPermission> RoleMenuPermissions { get; set; } = new List<RoleMenuPermission>();
}
