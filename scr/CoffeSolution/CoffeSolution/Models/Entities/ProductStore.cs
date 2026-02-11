namespace CoffeSolution.Models.Entities;

/// <summary>
/// Quản lý tồn kho theo từng cửa hàng
/// - Giải quyết vấn đề Global Products dùng chung tồn kho
/// </summary>
public class ProductStore
{
    public int Id { get; set; }
    
    // Sản phẩm nào
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Cửa hàng nào
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    // Số lượng tồn kho tại cửa hàng này
    public int Quantity { get; set; } = 0;
    
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
