namespace CoffeSolution.Models.Entities;

/// <summary>
/// Phiếu nhập kho
/// </summary>
public class WarehouseReceipt
{
    public int Id { get; set; }
    public string ReceiptCode { get; set; } = null!;  // Mã phiếu: WH-20260203-001
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }

    // Thuộc Store nào
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    // Nhà cung cấp
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // Người tạo phiếu
    public int? CreatedById { get; set; }
    public User? CreatedBy { get; set; }

    // Navigation
    public ICollection<WarehouseReceiptDetail> Details { get; set; } = new List<WarehouseReceiptDetail>();
}
