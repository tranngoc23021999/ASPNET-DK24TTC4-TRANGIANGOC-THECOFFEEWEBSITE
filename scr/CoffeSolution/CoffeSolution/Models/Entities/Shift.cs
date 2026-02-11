using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoffeSolution.Models.Entities;

/// <summary>
/// Ca làm việc của nhân viên
/// </summary>
public class Shift
{
    [Key]
    public int Id { get; set; }

    // Người trực ca
    public int StaffId { get; set; }
    public User Staff { get; set; } = null!;

    // Cửa hàng
    public int StoreId { get; set; }
    public Store Store { get; set; } = null!;

    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime? EndTime { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal StartingCash { get; set; } // Tiền đầu ca

    [Column(TypeName = "decimal(18,2)")]
    public decimal? EndingCash { get; set; } // Tiền bàn giao (thực tế đếm được)

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRevenueCash { get; set; } // Doanh thu tiền mặt trong ca (tính theo Order)
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRevenueCard { get; set; } // Doanh thu thẻ trong ca
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalRevenueTransfer { get; set; } // Doanh thu chuyển khoản

    public string Status { get; set; } = "Open"; // Open, Closed

    public string? Note { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
