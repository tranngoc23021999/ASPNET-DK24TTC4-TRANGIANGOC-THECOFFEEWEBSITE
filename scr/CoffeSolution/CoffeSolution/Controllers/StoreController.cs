using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

public class StoreController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Store;

    public StoreController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index(string? search, string? status, int page = 1)
    {
        await SetPermissionViewBagAsync(_menuId);

        var query = _context.Stores
            .Include(s => s.Owner)
            .Include(s => s.UserStores)
            .AsQueryable();

        // Administrator thấy tất cả, Admin chỉ thấy store của mình
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin)
        {
            query = query.Where(s => s.OwnerId == CurrentUserId);
        }

        if (!string.IsNullOrEmpty(status))
        {
             if (status == "active") query = query.Where(s => s.IsActive);
             else if (status == "inactive") query = query.Where(s => !s.IsActive);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Code != null && s.Code.Contains(search)) ||
                (s.Address != null && s.Address.Contains(search)));
        }

        var stores = await query
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StoreListViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Address = s.Address,
                Phone = s.Phone,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt,
                OwnerName = s.Owner.FullName,
                StaffCount = s.UserStores.Count
            })
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(stores);
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Details(int id)
    {
        var store = await _context.Stores
            .Include(s => s.Owner)
            .Include(s => s.UserStores)
                .ThenInclude(us => us.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (store == null) return NotFound();

        // Kiểm tra quyền xem
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin && store.OwnerId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Auth");
        }

        await SetPermissionViewBagAsync(_menuId);
        return View(store);
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        var isAdministrator = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        ViewBag.IsAdministrator = isAdministrator;
        
        if (isAdministrator)
        {
            ViewBag.Admins = await GetAdminSelectListAsync();
        }
        
        return View(new StoreViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken] 
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(StoreViewModel model)
    {
        var isAdministrator = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        
        if (!ModelState.IsValid)
        {
            ViewBag.IsAdministrator = isAdministrator;
            if (isAdministrator)
            {
                ViewBag.Admins = await GetAdminSelectListAsync();
            }
            return View(model);
        }

        int ownerId;
        if (isAdministrator && model.OwnerId.HasValue)
        {
            ownerId = model.OwnerId.Value;
        }
        else
        {
            ownerId = CurrentUserId!.Value;
        }

        var store = new Store
        {
            Name = model.Name,
            Code = model.Code,
            Address = model.Address,
            Phone = model.Phone,
            Email = model.Email,
            IsActive = model.IsActive,
            OwnerId = ownerId,
            CreatedAt = DateTime.Now
        };

        _context.Stores.Add(store);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Tạo cửa hàng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var store = await _context.Stores.FindAsync(id);
        if (store == null) return NotFound();

        // Kiểm tra quyền sửa
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin && store.OwnerId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Auth");
        }

        var model = new StoreViewModel
        {
            Id = store.Id,
            Name = store.Name,
            Code = store.Code,
            Address = store.Address,
            Phone = store.Phone,
            Email = store.Email,
            IsActive = store.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, StoreViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var store = await _context.Stores.FindAsync(id);
        if (store == null) return NotFound();

        // Kiểm tra quyền sửa
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin && store.OwnerId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Auth");
        }

        store.Name = model.Name;
        store.Code = model.Code;
        store.Address = model.Address;
        store.Phone = model.Phone;
        store.Email = model.Email;
        store.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Cập nhật cửa hàng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var store = await _context.Stores
            .Include(s => s.Products)
            .Include(s => s.Orders)
            .Include(s => s.UserStores)
            .Include(s => s.WarehouseReceipts)
            .Include(s => s.Suppliers)
            .FirstOrDefaultAsync(s => s.Id == id); // Need to include related items to check count

        if (store == null) return NotFound();

        // Kiểm tra quyền xóa
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin && store.OwnerId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Auth");
        }

        // Check dependencies
        if (store.Products.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa cửa hàng đang có sản phẩm!";
            return RedirectToAction(nameof(Index));
        }

        if (store.Orders.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa cửa hàng đã có đơn hàng!";
            return RedirectToAction(nameof(Index));
        }

        if (store.UserStores.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa cửa hàng đang có nhân viên!";
            return RedirectToAction(nameof(Index));
        }

        if (store.WarehouseReceipts.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa cửa hàng đã có phiếu nhập kho!";
            return RedirectToAction(nameof(Index));
        }
        
        if (store.Suppliers.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa cửa hàng đang có nhà cung cấp!";
            return RedirectToAction(nameof(Index));
        }

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa cửa hàng thành công!";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Lấy danh sách Admin users (không phải Administrator) để gắn làm Owner
    /// </summary>
    private async Task<List<SelectListItem>> GetAdminSelectListAsync()
    {
        // Lấy role "Admin" (không phải Administrator)
        var adminRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Admin");

        if (adminRole == null)
        {
            return new List<SelectListItem>();
        }

        // Lấy tất cả users có role Admin
        var adminUsers = await _context.UserRoles
            .Where(ur => ur.RoleId == adminRole.Id)
            .Select(ur => ur.User)
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = $"{u.FullName} ({u.Username})"
            })
            .ToListAsync();

        return adminUsers;
    }
}
