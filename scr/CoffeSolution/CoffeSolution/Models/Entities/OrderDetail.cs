namespace CoffeSolution.Models.Entities;

/// <summary>
/// Chi tiết đơn hàng
/// </summary>
public class OrderDetail
{
    public int Id { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // Giá tại thời điểm bán
    public decimal Amount { get; set; }  // = Quantity * UnitPrice
    public string? Note { get; set; }  // Ghi chú: ít đường, nhiều đá...

    // Thuộc Order nào
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    // Sản phẩm nào
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
