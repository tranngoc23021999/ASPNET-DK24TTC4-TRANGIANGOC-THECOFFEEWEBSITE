using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

public class ReportController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Report;

    public ReportController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index()
    {
        await SetPermissionViewBagAsync(_menuId);

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var allowedStoreIds = isAdmin ? null : await GetAllowedStoreIdsAsync();

        // Dashboard data
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var ordersQuery = _context.Orders.AsQueryable();
        if (allowedStoreIds != null)
            ordersQuery = ordersQuery.Where(o => allowedStoreIds.Contains(o.StoreId));

        // Tổng quan hôm nay
        ViewBag.TodayOrders = await ordersQuery
            .Where(o => o.CreatedAt >= today && o.Status == "Completed")
            .CountAsync();

        ViewBag.TodayRevenue = await ordersQuery
            .Where(o => o.CreatedAt >= today && o.Status == "Completed")
            .SumAsync(o => o.TotalAmount);

        // Tổng quan tháng này
        ViewBag.MonthOrders = await ordersQuery
            .Where(o => o.CreatedAt >= startOfMonth && o.Status == "Completed")
            .CountAsync();

        ViewBag.MonthRevenue = await ordersQuery
            .Where(o => o.CreatedAt >= startOfMonth && o.Status == "Completed")
            .SumAsync(o => o.TotalAmount);

        // Số liệu khác
        var storesQuery = _context.Stores.AsQueryable();
        var productsQuery = _context.Products.AsQueryable();

        if (allowedStoreIds != null)
        {
            storesQuery = storesQuery.Where(s => allowedStoreIds.Contains(s.Id));
            productsQuery = productsQuery.Where(p => allowedStoreIds.Contains(p.StoreId));
        }

        ViewBag.TotalStores = await storesQuery.CountAsync();
        ViewBag.TotalProducts = await productsQuery.CountAsync();
        ViewBag.LowStockProducts = await productsQuery.Where(p => p.StockQuantity <= 10).CountAsync();

        return View();
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Sales(int? storeId, DateTime? fromDate, DateTime? toDate)
    {
        await SetPermissionViewBagAsync(_menuId);

        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);

        var query = _context.Orders
            .Include(o => o.Store)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to.AddDays(1) && o.Status == "Completed");

        if (!isAdmin)
        {
            var allowedStoreIds = await GetAllowedStoreIdsAsync();
            query = query.Where(o => allowedStoreIds.Contains(o.StoreId));
        }

        if (storeId.HasValue)
            query = query.Where(o => o.StoreId == storeId);

        // Doanh thu theo ngày
        var dailySales = await query
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Doanh thu theo cửa hàng
        var storeSales = await query
            .GroupBy(o => o.Store.Name)
            .Select(g => new
            {
                StoreName = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.StoreId = storeId;
        ViewBag.Stores = await GetStoreSelectListAsync();
        ViewBag.DailySales = dailySales;
        ViewBag.StoreSales = storeSales;
        ViewBag.TotalRevenue = storeSales.Sum(x => x.Revenue);
        ViewBag.TotalOrders = storeSales.Sum(x => x.OrderCount);

        return View();
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Products(int? storeId)
    {
        await SetPermissionViewBagAsync(_menuId);

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);

        var query = _context.Products
            .Include(p => p.Store)
            .AsQueryable();

        if (!isAdmin)
        {
            var allowedStoreIds = await GetAllowedStoreIdsAsync();
            query = query.Where(p => allowedStoreIds.Contains(p.StoreId));
        }

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId);

        var products = await query
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        ViewBag.StoreId = storeId;
        ViewBag.Stores = await GetStoreSelectListAsync();

        return View(products);
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
}
