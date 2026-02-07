namespace CoffeSolution.Models.Entities;

/// <summary>
/// Người dùng hệ thống: Administrator, Admin, Leader, Staff
/// - User có nhiều Role (Many-to-Many)
/// - User thuộc nhiều Store (Many-to-Many)
/// - User có thể được quản lý bởi Admin khác (AdminId)
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Người quản lý (Admin/Leader) - null nếu là Administrator
    public int? AdminId { get; set; }
    public User? Admin { get; set; }

    // Nhân viên được quản lý bởi user này
    public ICollection<User> ManagedUsers { get; set; } = new List<User>();

    // Many-to-Many: User có nhiều Role
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Many-to-Many: User thuộc nhiều Store
    public ICollection<UserStore> UserStores { get; set; } = new List<UserStore>();

    // Đơn hàng do user tạo
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
