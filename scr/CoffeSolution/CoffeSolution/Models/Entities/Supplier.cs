namespace CoffeSolution.Models.Entities;

/// <summary>
/// Nhà cung cấp nguyên liệu
/// </summary>
public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? ContactPerson { get; set; }  // Người liên hệ
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Thuộc Store nào (Null = Global/System-wide)
    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    // Phân loại Global
    public bool IsSystem { get; set; } = false; // True = Administrator (All Stores), False = Standard
    public int? CreatedById { get; set; } // Track Owner for Owner-Global items

    // Navigation
    public ICollection<WarehouseReceipt> WarehouseReceipts { get; set; } = new List<WarehouseReceipt>();
}
