using System.ComponentModel.DataAnnotations;

namespace CoffeSolution.Models.Entities;

public class Customer
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã khách hàng là bắt buộc")]
    [Display(Name = "Mã khách hàng")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [Display(Name = "Tên khách hàng")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = null!;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [Display(Name = "Cửa hàng")]
    public int? StoreId { get; set; }
    public Store? Store { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
