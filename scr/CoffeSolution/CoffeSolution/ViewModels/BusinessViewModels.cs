using System.ComponentModel.DataAnnotations;

namespace CoffeSolution.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
    [Display(Name = "Tên sản phẩm")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập giá")]
    [Display(Name = "Giá bán")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
    public decimal Price { get; set; }

    [Display(Name = "Hình ảnh")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Danh mục")]
    public string? Category { get; set; }

    [Display(Name = "Tồn kho")]
    public int StockQuantity { get; set; }

    [Display(Name = "Đơn vị")]
    public string? Unit { get; set; }

    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Vui lòng chọn cửa hàng")]
    [Display(Name = "Cửa hàng")]
    public int StoreId { get; set; }
}

public class SupplierViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp")]
    [Display(Name = "Tên nhà cung cấp")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Display(Name = "Địa chỉ")]
    [StringLength(200)]
    public string? Address { get; set; }

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Display(Name = "Người liên hệ")]
    public string? ContactPerson { get; set; }

    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; } = true;
}

public class OrderViewModel
{
    public int Id { get; set; }
    public string OrderCode { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentMethod { get; set; } = "Cash";
    public string? Note { get; set; }
    public int StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? StaffName { get; set; }
    public List<OrderDetailViewModel> Items { get; set; } = new();
}

public class OrderDetailViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class UserViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = null!;

    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = null!;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Display(Name = "Trạng thái")]  
    public bool IsActive { get; set; } = true;

    [Display(Name = "Người quản lý")]
    public int? AdminId { get; set; }

    [Display(Name = "Vai trò")]
    public List<int> RoleIds { get; set; } = new();

    [Display(Name = "Cửa hàng")]
    public List<int> StoreIds { get; set; } = new();
}

public class RoleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên vai trò")]
    [Display(Name = "Tên vai trò")]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // Dictionary: MenuCode -> List<ActionCode>
    // VD: { "ORDER": ["VIEW", "CREATE"], "PRODUCT": ["VIEW"] }
    public Dictionary<string, List<string>> Permissions { get; set; } = new();
}
