namespace CoffeSolution.Models.Entities;

/// <summary>
/// Bảng junction: User ↔ Store (Many-to-Many)
/// User có thể thuộc nhiều Store, Store có nhiều User
/// </summary>
public class UserStore
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public bool IsDefault { get; set; } = false;  // Store mặc định khi login
    public DateTime AssignedAt { get; set; } = DateTime.Now;
}
