using System.ComponentModel.DataAnnotations;
using CoffeSolution.Models.Entities;

namespace CoffeSolution.ViewModels;

#region User ViewModels

/// <summary>
/// ViewModel cho danh sách users
/// </summary>
public class UserListViewModel
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RoleName { get; set; }
    public List<string> StoreNames { get; set; } = new();
}

/// <summary>
/// ViewModel cho tạo mới user
/// </summary>
public class CreateUserViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [Display(Name = "Tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3-50 ký tự")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Tên đăng nhập chỉ chứa chữ, số và dấu gạch dưới")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string Password { get; set; } = null!;

    [Display(Name = "Xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [Display(Name = "Họ tên")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public int RoleId { get; set; }

    /// <summary>
    /// Cửa hàng - Bắt buộc cho Leader/Staff, không bắt buộc cho Admin
    /// </summary>
    [Display(Name = "Cửa hàng")]
    public List<int> StoreIds { get; set; } = new();

    [Display(Name = "Yêu cầu đổi mật khẩu lần đầu")]
    public bool MustChangePassword { get; set; } = true;

    // Cho Select list
    public List<Role> AvailableRoles { get; set; } = new();
    public List<Store> AvailableStores { get; set; } = new();
}

/// <summary>
/// ViewModel cho chỉnh sửa user
/// </summary>
public class EditUserViewModel
{
    public int Id { get; set; }

    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [Display(Name = "Họ tên")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [Display(Name = "Số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public int RoleId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ít nhất 1 cửa hàng")]
    [Display(Name = "Cửa hàng")]
    public List<int> StoreIds { get; set; } = new();

    [Display(Name = "Trạng thái")]
    public bool IsActive { get; set; } = true;

    // Reset password (optional)
    [Display(Name = "Mật khẩu mới")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    public string? NewPassword { get; set; }

    // Cho Select list
    public List<Role> AvailableRoles { get; set; } = new();
    public List<Store> AvailableStores { get; set; } = new();
}

#endregion

#region Role ViewModels

/// <summary>
/// ViewModel cho danh sách roles
/// </summary>
public class RoleListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// ViewModel cho tạo/sửa role với permissions
/// </summary>
public class RolePermissionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên vai trò")]
    [Display(Name = "Tên vai trò")]
    [StringLength(50)]
    public string Name { get; set; } = null!;

    [Display(Name = "Mô tả")]
    [StringLength(200)]
    public string? Description { get; set; }

    // Permissions: MenuId -> List<ActionId>
    public List<MenuPermissionItem> MenuPermissions { get; set; } = new();
}

/// <summary>
/// Item cho mỗi menu trong permission assignment
/// </summary>
public class MenuPermissionItem
{
    public int MenuId { get; set; }
    public string MenuCode { get; set; } = null!;
    public string MenuName { get; set; } = null!;
    public List<ActionPermissionItem> Actions { get; set; } = new();
}

/// <summary>
/// Item cho mỗi action trong permission
/// </summary>
public class ActionPermissionItem
{
    public int ActionId { get; set; }
    public string ActionCode { get; set; } = null!;
    public string ActionName { get; set; } = null!;
    public bool IsGranted { get; set; }
}

#endregion

#region Store Selection ViewModels

/// <summary>
/// ViewModel cho màn hình chọn store sau khi login
/// </summary>
public class SelectStoreViewModel
{
    public List<StoreOptionItem> Stores { get; set; } = new();
    public int? SelectedStoreId { get; set; }
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Item cho dropdown chọn store
/// </summary>
public class StoreOptionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// ViewModel cho Store Filter (Admin/Administrator)
/// </summary>
public class StoreFilterViewModel
{
    public int? CurrentFilterId { get; set; }
    public string CurrentFilterType { get; set; } = "all"; // "all", "admin", "store"
    public string CurrentFilterName { get; set; } = "Tất cả";
    
    // Cho Administrator: filter theo Admin
    public List<AdminFilterItem> AdminFilters { get; set; } = new();
    
    // Filter theo Store
    public List<StoreOptionItem> StoreFilters { get; set; } = new();
    
    public bool IsAdministrator { get; set; }
    public bool IsAdmin { get; set; }
}

/// <summary>
/// Item cho filter theo Admin (dành cho Administrator)
/// </summary>
public class AdminFilterItem
{
    public int AdminId { get; set; }
    public string AdminName { get; set; } = null!;
    public int StoreCount { get; set; }
}

#endregion

#region Store Selector ViewComponent ViewModel

/// <summary>
/// ViewModel cho StoreSelectorViewComponent
/// </summary>
public class StoreSelectorViewModel
{
    public List<StoreOptionItem> Stores { get; set; } = new();
    public int? CurrentStoreId { get; set; }
    public string CurrentStoreName { get; set; } = "Chọn cửa hàng";
}

#endregion

#region User Index ViewModel

/// <summary>
/// ViewModel tổng hợp cho trang User/Index
/// </summary>
public class UserIndexViewModel
{
    public List<UserListViewModel> Users { get; set; } = new();
    public StoreFilterViewModel Filter { get; set; } = new();
    
    // Search & pagination
    public string? SearchTerm { get; set; }
    public int? FilterRoleId { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

#endregion
