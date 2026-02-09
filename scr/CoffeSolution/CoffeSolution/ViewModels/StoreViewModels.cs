using System.ComponentModel.DataAnnotations;

namespace CoffeSolution.ViewModels;

public class StoreViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng")]
    [Display(Name = "Tên cửa hàng")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Display(Name = "Mã cửa hàng")]
    [StringLength(20)]
    public string? Code { get; set; }

    [Display(Name = "Địa chỉ")]
    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; } = true;
}

public class StoreListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string OwnerName { get; set; } = null!;
    public int StaffCount { get; set; }
}
