using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Index(string? search, int page = 1)
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
    public IActionResult Create()
    {
        return View(new StoreViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(StoreViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var store = new Store
        {
            Name = model.Name,
            Code = model.Code,
            Address = model.Address,
            Phone = model.Phone,
            Email = model.Email,
            IsActive = model.IsActive,
            OwnerId = CurrentUserId!.Value,
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
        var store = await _context.Stores.FindAsync(id);
        if (store == null) return NotFound();

        // Kiểm tra quyền xóa
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin && store.OwnerId != CurrentUserId)
        {
            return RedirectToAction("AccessDenied", "Auth");
        }

        _context.Stores.Remove(store);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa cửa hàng thành công!";
        return RedirectToAction(nameof(Index));
    }
}
