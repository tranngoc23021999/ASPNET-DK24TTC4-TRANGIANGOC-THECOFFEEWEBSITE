namespace CoffeSolution.Models.Entities;

/// <summary>
/// Đơn hàng bán hàng
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = null!;  // Mã đơn: ORD-20260203-001
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";  // Pending, Completed, Cancelled
    public string PaymentMethod { get; set; } = "Cash";  // Cash, Card, Transfer
    public string? Note { get; set; }

    // Thuộc Store nào
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    // Nhân viên tạo đơn
    public int? StaffId { get; set; }
    public User? Staff { get; set; }

    // Navigation
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
