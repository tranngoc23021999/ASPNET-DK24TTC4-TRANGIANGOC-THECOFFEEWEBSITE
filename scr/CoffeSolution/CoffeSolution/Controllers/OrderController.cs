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

public class OrderController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Order;
    public OrderController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index(string? search, int? storeId, string? status, DateTime? fromDate, DateTime? toDate)
    {
        await SetPermissionViewBagAsync(_menuId);

        var query = _context.Orders
            .Include(o => o.Store)
            .Include(o => o.Staff)
            .AsQueryable();

        // Lọc theo store mà user có quyền
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin)
        {
            var allowedStoreIds = await GetAllowedStoreIdsAsync();
            query = query.Where(o => allowedStoreIds.Contains(o.StoreId));
        }

        if (storeId.HasValue)
            query = query.Where(o => o.StoreId == storeId);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(o => o.Status == status);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));

        if (!string.IsNullOrEmpty(search))
            query = query.Where(o => o.OrderCode.Contains(search));

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(100)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.StoreId = storeId;
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Stores = await GetStoreSelectListAsync();
        ViewBag.Statuses = GetStatusSelectList();

        return View(orders);
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Store)
            .Include(o => o.Staff)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        await SetPermissionViewBagAsync(MenuCode.Order);
        return View(order);
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Stores = await GetStoreSelectListAsync();
        return View(new OrderPosViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(OrderPosViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreSelectListAsync();
            return View(model);
        }

        var order = new Order
        {
            OrderCode = GenerateOrderCode(),
            StoreId = model.StoreId,
            StaffId = CurrentUserId,
            TotalAmount = model.TotalAmount,
            Status = "Pending",
            PaymentMethod = model.PaymentMethod,
            Note = model.Note,
            CreatedAt = DateTime.Now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Tạo đơn hàng thành công!";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Cập nhật trạng thái thành công!";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        // Chỉ cho phép xóa đơn hàng Pending (sai sót khi tạo)
        if (order.Status != "Pending")
        {
            TempData[TempDataKey.Error] = "Chỉ có thể xóa đơn hàng đang chờ xử lý (Pending)!";
            return RedirectToAction(nameof(Index));
        }

        _context.OrderDetails.RemoveRange(order.OrderDetails);
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa đơn hàng thành công!";
        return RedirectToAction(nameof(Index));
    }

    private string GenerateOrderCode()
    {
        return $"ORD-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";
    }

    private async Task<List<int>> GetAllowedStoreIdsAsync()
    {
        var userStoreIds = await _context.UserStores
            .Where(us => us.UserId == CurrentUserId)
            .Select(us => us.StoreId)
            .ToListAsync();

        var ownedStoreIds = await _context.Stores
            .Where(s => s.OwnerId == CurrentUserId)
            .Select(s => s.Id)
            .ToListAsync();

        return userStoreIds.Union(ownedStoreIds).ToList();
    }

    private async Task<List<SelectListItem>> GetStoreSelectListAsync()
    {
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var query = _context.Stores.Where(s => s.IsActive);

        if (!isAdmin)
        {
            var allowedStoreIds = await GetAllowedStoreIdsAsync();
            query = query.Where(s => allowedStoreIds.Contains(s.Id));
        }

        return await query
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
    }

    private List<SelectListItem> GetStatusSelectList()
    {
        return new List<SelectListItem>
        {
            new("Chờ xử lý", "Pending"),
            new("Đang làm", "Processing"),
            new("Hoàn thành", "Completed"),
            new("Đã hủy", "Cancelled")
        };
    }
}
