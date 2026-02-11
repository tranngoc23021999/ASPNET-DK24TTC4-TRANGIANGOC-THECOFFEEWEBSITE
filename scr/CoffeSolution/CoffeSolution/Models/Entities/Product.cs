namespace CoffeSolution.Models.Entities;

/// <summary>
/// Sản phẩm (Menu Coffee) - mỗi Store có danh sách sản phẩm riêng
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Category { get; set; }  // Coffee, Tea, Juice, Food...
    public int StockQuantity { get; set; } = 0;  // Tồn kho đơn giản
    public bool AllowNegativeStock { get; set; } = false; // Cho phép bán âm kho
    public string? Unit { get; set; } = "Ly";  // Đơn vị: Ly, Phần, Gói...
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Thuộc Store nào (Null = Global/System-wide)
    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    // Phân loại Global
    public bool IsSystem { get; set; } = false; // True = Administrator (All Stores), False = Standard
    public int? CreatedById { get; set; } // Track Owner for Owner-Global items

    // Navigation
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<WarehouseReceiptDetail> WarehouseReceiptDetails { get; set; } = new List<WarehouseReceiptDetail>();
    public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
}
