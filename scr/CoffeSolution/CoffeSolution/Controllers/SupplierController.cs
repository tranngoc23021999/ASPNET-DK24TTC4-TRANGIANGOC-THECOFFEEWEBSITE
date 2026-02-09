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

public class SupplierController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Supplier;

    public SupplierController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index(string? search)
    {
        await SetPermissionViewBagAsync(_menuId);

        var query = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Phone != null && s.Phone.Contains(search)) ||
                (s.ContactPerson != null && s.ContactPerson.Contains(search)));
        }

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin)
        {
            var userStoreIds = await _context.UserStores
                .Where(us => us.UserId == CurrentUserId)
                .Select(us => us.StoreId)
                .ToListAsync();

            var ownedStores = await _context.Stores
                .Where(s => s.OwnerId == CurrentUserId)
                .Select(s => new { s.Id, s.OwnerId })
                .ToListAsync();

            var ownedStoreIds = ownedStores.Select(s => s.Id).ToList();
            var allowedStoreIds = userStoreIds.Union(ownedStoreIds).Cast<int?>().ToList();
            
            var ownerIds = await _context.Stores
                .Where(s => allowedStoreIds.Contains(s.Id))
                .Select(s => s.OwnerId)
                .Distinct()
                .ToListAsync();
            
             if (User.IsInRole("Admin")) 
             {
                 ownerIds.Add(CurrentUserId!.Value);
             }

            query = query.Where(s => 
                s.IsSystem == true || 
                allowedStoreIds.Contains(s.StoreId) ||
                (s.StoreId == null && s.CreatedById.HasValue && ownerIds.Contains(s.CreatedById.Value))
            );
        }

        var suppliers = await query
            .Include(s => s.Store)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        return View(suppliers);
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Stores = await GetStoreSelectListAsync();
        return View("CreateEdit", new SupplierViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(SupplierViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreSelectListAsync();
            return View("CreateEdit", model);
        }

        // Validate and Setup Supplier Scoping
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);

        if (model.StoreId == null)
        {
            if (isAdmin)
            {
                // Administrator -> System Global
                model.IsSystem = true;
            }
            else
            {
                // Check if user is an Owner (Admin role)
                var isOwner = User.IsInRole("Admin");
                if (isOwner)
                {
                    // Owner -> Owner Global
                    model.IsSystem = false;
                    model.CreatedById = CurrentUserId;
                }
                else
                {
                    ModelState.AddModelError("StoreId", "Bạn không có quyền tạo nhà cung cấp toàn cục.");
                    ViewBag.Stores = await GetStoreSelectListAsync();
                    return View("CreateEdit", model);
                }
            }
        }
        else
        {
             // Store specific - check permission
             if (!isAdmin)
             {
                 var allowedStoreIds = await GetAllowedStoreIdsAsync(_context);
                 if (!allowedStoreIds.Contains(model.StoreId.Value))
                 {
                      ModelState.AddModelError("StoreId", "Bạn không có quyền tạo nhà cung cấp cho cửa hàng này.");
                      return View("CreateEdit", model);
                 }
             }
        }

        var supplier = new Supplier
        {
            Name = model.Name,
            Address = model.Address,
            Phone = model.Phone,
            Email = model.Email,
            ContactPerson = model.ContactPerson,
            IsActive = model.IsActive,
            StoreId = model.StoreId,
            IsSystem = model.IsSystem,
            CreatedById = model.CreatedById ?? CurrentUserId,
            CreatedAt = DateTime.Now
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Tạo nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        var model = new SupplierViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Address = supplier.Address,
            Phone = supplier.Phone,
            Email = supplier.Email,
            ContactPerson = supplier.ContactPerson,
            IsActive = supplier.IsActive,
            StoreId = supplier.StoreId,
            CreatedAt = supplier.CreatedAt
        };
        
        ViewBag.Stores = await GetStoreSelectListAsync();
        return View("CreateEdit", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, SupplierViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreSelectListAsync();
            return View("CreateEdit", model);
        }

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        supplier.Name = model.Name;
        supplier.Address = model.Address;
        supplier.Phone = model.Phone;
        supplier.Email = model.Email;
        supplier.ContactPerson = model.ContactPerson;
        supplier.IsActive = model.IsActive;
        supplier.StoreId = model.StoreId;

        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Cập nhật nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.WarehouseReceipts)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (supplier == null) return NotFound();

        // Check dependencies
        if (supplier.WarehouseReceipts.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa nhà cung cấp đã có phiếu nhập kho!";
            return RedirectToAction(nameof(Index));
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetStoreSelectListAsync()
    {
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var query = _context.Stores.Where(s => s.IsActive);

        if (!isAdmin)
        {
            var allowedStoreIds = await GetAllowedStoreIdsAsync(_context);
            query = query.Where(s => allowedStoreIds.Contains(s.Id));
        }

        var stores = await query
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();

        if (isAdmin)
        {
            stores.Insert(0, new SelectListItem { Value = "", Text = "Áp dụng cho tất cả cửa hàng (Toàn hệ thống)" });
        }
        else if (User.IsInRole("Admin")) // Owner
        {
            stores.Insert(0, new SelectListItem { Value = "", Text = "Áp dụng cho tất cả cửa hàng của tôi" });
        }

        return stores;
    }
}
