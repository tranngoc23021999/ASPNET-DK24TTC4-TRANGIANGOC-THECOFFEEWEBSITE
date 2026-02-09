using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

/// <summary>
/// Controller xử lý việc chọn Store và chuyển đổi Store
/// </summary>
[Authorize]
public class StoreSelectController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public StoreSelectController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
    }

    #region Select Store (Sau khi login)

    /// <summary>
    /// Màn hình chọn Store khi login (cho user có nhiều stores)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Select(string? returnUrl = null)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");

        // Administrator không cần chọn store
        var isAdministrator = await _permissionService.IsAdministratorAsync(userId.Value);
        if (isAdministrator)
        {
            return RedirectToLocal(returnUrl);
        }

        // Lấy stores: assigned (UserStores) + owned (OwnerId)
        var assignedStores = await _context.UserStores
            .Include(us => us.Store)
            .Where(us => us.UserId == userId && us.Store.IsActive)
            .Select(us => new { Store = us.Store, IsDefault = us.IsDefault })
            .ToListAsync();
        
        var ownedStores = await _context.Stores
            .Where(s => s.OwnerId == userId && s.IsActive)
            .Select(s => new { Store = s, IsDefault = false })
            .ToListAsync();
        
        // Merge và loại bỏ trùng lặp
        var allStores = assignedStores
            .Concat(ownedStores)
            .GroupBy(x => x.Store.Id)
            .Select(g => g.First())
            .OrderBy(x => x.Store.Name)
            .ToList();

        // Admin role không có store -> vẫn cho vào để tạo store
        var isAdmin = await IsAdminRoleAsync(userId.Value);
        if (!allStores.Any())
        {
            if (isAdmin)
            {
                // Admin chưa có store -> vẫn cho đăng nhập (sẽ tạo store sau)
                return RedirectToLocal(returnUrl);
            }
            TempData[TempDataKey.Error] = "Tài khoản chưa được gán vào cửa hàng nào!";
            return RedirectToAction("AccessDenied", "Auth");
        }

        // Nếu chỉ có 1 store -> Auto select
        if (allStores.Count == 1)
        {
            await SetCurrentStoreAsync(allStores[0].Store.Id);
            return RedirectToLocal(returnUrl);
        }

        // Nhiều stores -> Hiển thị màn hình chọn
        var viewModel = new SelectStoreViewModel
        {
            Stores = allStores.Select(x => new StoreOptionItem
            {
                Id = x.Store.Id,
                Name = x.Store.Name,
                Code = x.Store.Code,
                Address = x.Store.Address,
                IsDefault = x.IsDefault
            }).ToList(),
            SelectedStoreId = allStores.FirstOrDefault(x => x.IsDefault)?.Store.Id,
            ReturnUrl = returnUrl
        };

        return View(viewModel);
    }

    /// <summary>
    /// Xử lý chọn Store sau khi login
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(int storeId, string? returnUrl = null)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");

        // Validate store thuộc về user (assigned hoặc owned)
        var hasAccess = await _context.UserStores
            .AnyAsync(us => us.UserId == userId && us.StoreId == storeId);
        
        if (!hasAccess)
        {
            hasAccess = await _context.Stores
                .AnyAsync(s => s.Id == storeId && s.OwnerId == userId);
        }

        if (!hasAccess)
        {
            TempData[TempDataKey.Error] = "Bạn không có quyền truy cập cửa hàng này!";
            return RedirectToAction(nameof(Select), new { returnUrl });
        }

        await SetCurrentStoreAsync(storeId);
        return RedirectToLocal(returnUrl);
    }

    #endregion

    #region Switch Store (Chuyển đổi Store)

    /// <summary>
    /// Chuyển đổi Store khi đang làm việc (AJAX)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Switch(int storeId)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Json(new { success = false, message = "Phiên đăng nhập hết hạn" });

        var isAdministrator = await _permissionService.IsAdministratorAsync(userId.Value);

        // Administrator có thể xem bất kỳ store nào
        if (isAdministrator)
        {
            await SetCurrentStoreAsync(storeId);
            return Json(new { success = true });
        }

        // Validate store access (assigned hoặc owned)
        var hasAccess = await _context.UserStores
            .AnyAsync(us => us.UserId == userId && us.StoreId == storeId);
        
        if (!hasAccess)
        {
            hasAccess = await _context.Stores
                .AnyAsync(s => s.Id == storeId && s.OwnerId == userId);
        }

        if (!hasAccess)
        {
            return Json(new { success = false, message = "Bạn không có quyền truy cập cửa hàng này" });
        }

        await SetCurrentStoreAsync(storeId);
        return Json(new { success = true });
    }

    /// <summary>
    /// Clear Store filter (cho Admin/Administrator xem tất cả)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearFilter()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Json(new { success = false });

        var isAdministrator = await _permissionService.IsAdministratorAsync(userId.Value);
        var isAdmin = await IsAdminRoleAsync(userId.Value);

        // Chỉ Admin và Administrator mới được clear filter
        if (!isAdministrator && !isAdmin)
        {
            return Json(new { success = false, message = "Bạn không có quyền xem tất cả cửa hàng" });
        }

        HttpContext.Session.Remove("CurrentStoreId");
        HttpContext.Session.SetString("StoreFilterType", "all");

        return Json(new { success = true });
    }

    /// <summary>
    /// Filter theo Admin (cho Administrator xem stores của 1 Admin)
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FilterByAdmin(int adminId)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Json(new { success = false });

        var isAdministrator = await _permissionService.IsAdministratorAsync(userId.Value);
        if (!isAdministrator)
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện" });
        }

        HttpContext.Session.SetInt32("FilterAdminId", adminId);
        HttpContext.Session.SetString("StoreFilterType", "admin");
        HttpContext.Session.Remove("CurrentStoreId");

        return Json(new { success = true });
    }

    #endregion

    #region API - Get Stores

    /// <summary>
    /// Lấy danh sách stores của user hiện tại (cho dropdown)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyStores()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Json(new { success = false });

        // Assigned stores
        var assignedStores = await _context.UserStores
            .Include(us => us.Store)
            .Where(us => us.UserId == userId && us.Store.IsActive)
            .Select(us => new
            {
                id = us.Store.Id,
                name = us.Store.Name,
                code = us.Store.Code,
                isDefault = us.IsDefault
            })
            .ToListAsync();
        
        // Owned stores
        var ownedStores = await _context.Stores
            .Where(s => s.OwnerId == userId && s.IsActive)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                code = s.Code,
                isDefault = false
            })
            .ToListAsync();
        
        // Merge và loại bỏ trùng lặp
        var stores = assignedStores
            .Concat(ownedStores)
            .GroupBy(x => x.id)
            .Select(g => g.First())
            .ToList();

        var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");

        return Json(new
        {
            success = true,
            stores = stores,
            currentStoreId = currentStoreId
        });
    }

    /// <summary>
    /// Lấy filter options cho Administrator
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFilterOptions()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Json(new { success = false });

        var isAdministrator = await _permissionService.IsAdministratorAsync(userId.Value);
        var isAdmin = await IsAdminRoleAsync(userId.Value);

        var result = new StoreFilterViewModel
        {
            IsAdministrator = isAdministrator,
            IsAdmin = isAdmin,
            CurrentFilterType = HttpContext.Session.GetString("StoreFilterType") ?? "all",
            CurrentFilterId = HttpContext.Session.GetInt32("CurrentStoreId")
        };

        if (isAdministrator)
        {
            // Lấy danh sách Admins
            result.AdminFilters = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserStores)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                .Select(u => new AdminFilterItem
                {
                    AdminId = u.Id,
                    AdminName = u.FullName,
                    StoreCount = u.UserStores.Count
                })
                .ToListAsync();

            // Lấy tất cả Stores
            result.StoreFilters = await _context.Stores
                .Where(s => s.IsActive)
                .Select(s => new StoreOptionItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Address = s.Address
                })
                .ToListAsync();
        }
        else if (isAdmin)
        {
            // Lấy stores của Admin
            result.StoreFilters = await _context.UserStores
                .Include(us => us.Store)
                .Where(us => us.UserId == userId && us.Store.IsActive)
                .Select(us => new StoreOptionItem
                {
                    Id = us.Store.Id,
                    Name = us.Store.Name,
                    Code = us.Store.Code,
                    Address = us.Store.Address
                })
                .ToListAsync();
        }

        return Json(new { success = true, filter = result });
    }

    #endregion

    #region Helper Methods

    private async Task SetCurrentStoreAsync(int storeId)
    {
        HttpContext.Session.SetInt32("CurrentStoreId", storeId);
        HttpContext.Session.SetString("StoreFilterType", "store");

        // Lấy tên store để hiển thị
        var store = await _context.Stores.FindAsync(storeId);
        if (store != null)
        {
            HttpContext.Session.SetString("CurrentStoreName", store.Name);
        }
    }

    private async Task<bool> IsAdminRoleAsync(int userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Admin");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    #endregion
}
