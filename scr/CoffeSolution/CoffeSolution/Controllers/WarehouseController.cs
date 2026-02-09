using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

public class WarehouseController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Warehouse;

    public WarehouseController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index(int? storeId, DateTime? fromDate, DateTime? toDate)
    {
        await SetPermissionViewBagAsync(_menuId);

        var query = _context.WarehouseReceipts
            .Include(wr => wr.Store)
            .Include(wr => wr.Supplier)
            .Include(wr => wr.CreatedBy)
            .AsQueryable();

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin)
        {
            var allowedStoreIds = await GetAllowedStoreIdsAsync();
            query = query.Where(wr => allowedStoreIds.Contains(wr.StoreId));
        }

        if (storeId.HasValue)
            query = query.Where(wr => wr.StoreId == storeId);

        if (fromDate.HasValue)
            query = query.Where(wr => wr.CreatedAt >= fromDate);

        if (toDate.HasValue)
            query = query.Where(wr => wr.CreatedAt <= toDate.Value.AddDays(1));

        var receipts = await query
            .OrderByDescending(wr => wr.CreatedAt)
            .Take(100)
            .ToListAsync();

        ViewBag.StoreId = storeId;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Stores = await GetStoreSelectListAsync();

        return View(receipts);
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Details(int id)
    {
        var receipt = await _context.WarehouseReceipts
            .Include(wr => wr.Store)
            .Include(wr => wr.Supplier)
            .Include(wr => wr.CreatedBy)
            .Include(wr => wr.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(wr => wr.Id == id);

        if (receipt == null) return NotFound();

        await SetPermissionViewBagAsync(_menuId);
        return View(receipt);
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Stores = await GetStoreSelectListAsync();
        ViewBag.Suppliers = await GetSupplierSelectListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(int storeId, int supplierId, string? note, List<WarehouseReceiptDetailInput> items)
    {
        if (!items.Any())
        {
            TempData[TempDataKey.Error] = "Vui lòng thêm ít nhất một sản phẩm!";
            ViewBag.Stores = await GetStoreSelectListAsync();
            ViewBag.Suppliers = await GetSupplierSelectListAsync();
            return View();
        }

        var receipt = new WarehouseReceipt
        {
            ReceiptCode = $"WH-{DateTime.Now:yyyyMMdd-HHmmss}",
            StoreId = storeId,
            SupplierId = supplierId,
            Note = note,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
            CreatedById = CurrentUserId!.Value,
            CreatedAt = DateTime.Now
        };

        _context.WarehouseReceipts.Add(receipt);
        await _context.SaveChangesAsync();

        // Thêm chi tiết và cập nhật tồn kho
        foreach (var item in items)
        {
            var detail = new WarehouseReceiptDetail
            {
                WarehouseReceiptId = receipt.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = item.Quantity * item.UnitPrice
            };
            _context.WarehouseReceiptDetails.Add(detail);

            // Cập nhật tồn kho
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity += item.Quantity;
            }
        }

        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Nhập kho thành công!";
        return RedirectToAction(nameof(Details), new { id = receipt.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var receipt = await _context.WarehouseReceipts
            .Include(wr => wr.Details)
            .FirstOrDefaultAsync(wr => wr.Id == id);

        if (receipt == null) return NotFound();

        // Hoàn lại tồn kho
        foreach (var detail in receipt.Details)
        {
            var product = await _context.Products.FindAsync(detail.ProductId);
            if (product != null)
            {
                product.StockQuantity -= detail.Quantity;
            }
        }

        _context.WarehouseReceiptDetails.RemoveRange(receipt.Details);
        _context.WarehouseReceipts.Remove(receipt);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa phiếu nhập kho thành công!";
        return RedirectToAction(nameof(Index));
    }

    // API lấy products theo store
    [HttpGet]
    public async Task<IActionResult> GetProducts(int storeId)
    {
        var products = await _context.Products
            .Where(p => p.StoreId == storeId && p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Price, p.StockQuantity })
            .ToListAsync();

        return Json(products);
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

    private async Task<List<SelectListItem>> GetSupplierSelectListAsync()
    {
        return await _context.Suppliers
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
    }
}

public class WarehouseReceiptDetailInput
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
