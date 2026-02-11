namespace CoffeSolution.Models.Entities;

/// <summary>
/// Cửa hàng
/// - Store có nhiều User (Many-to-Many qua UserStore)
/// - Store thuộc về Admin (OwnerId)
/// </summary>
public class Store
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }  // Mã cửa hàng: CH01, CH02...
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Chủ cửa hàng (Admin)
    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    // Many-to-Many: Store có nhiều User
    public ICollection<UserStore> UserStores { get; set; } = new List<UserStore>();

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<WarehouseReceipt> WarehouseReceipts { get; set; } = new List<WarehouseReceipt>();
    public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
}
