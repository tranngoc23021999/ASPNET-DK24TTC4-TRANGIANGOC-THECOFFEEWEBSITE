namespace CoffeSolution.Models.Entities;

/// <summary>
/// Vai trò người dùng: Administrator, Admin, Leader, Staff
/// - Role có nhiều User (Many-to-Many)
/// - Role được gắn danh sách Menu + Permission
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false;  // Role hệ thống không được xóa
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Many-to-Many: Role có nhiều User
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Role được gắn nhiều (Menu + Permission)
    public ICollection<RoleMenuPermission> RoleMenuPermissions { get; set; } = new List<RoleMenuPermission>();
}
