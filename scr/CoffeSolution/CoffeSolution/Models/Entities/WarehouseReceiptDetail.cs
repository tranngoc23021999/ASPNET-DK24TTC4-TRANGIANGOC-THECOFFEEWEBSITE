namespace CoffeSolution.Models.Entities;

/// <summary>
/// Chi tiết phiếu nhập kho
/// </summary>
public class WarehouseReceiptDetail
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // Giá nhập
    public decimal Amount { get; set; }  // = Quantity * UnitPrice

    // Thuộc phiếu nào
    public int WarehouseReceiptId { get; set; }
    public WarehouseReceipt WarehouseReceipt { get; set; } = null!;

    // Sản phẩm nào
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
