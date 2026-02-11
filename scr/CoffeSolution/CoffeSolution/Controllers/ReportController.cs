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
    public async Task<IActionResult> Index(int? storeId)
    {
        await SetPermissionViewBagAsync(_menuId);

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var allowedStoreIds = isAdmin ? null : await GetAllowedStoreIdsAsync(_context);

        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        var ordersQuery = _context.Orders.AsQueryable();
        
        if (allowedStoreIds != null)
        {
            ordersQuery = ordersQuery.Where(o => allowedStoreIds.Contains(o.StoreId));
        }

        if (storeId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.StoreId == storeId);
        }

        ViewBag.TodayOrders = await ordersQuery
            .Where(o => o.CreatedAt >= today && o.Status == "Completed")
            .CountAsync();

        ViewBag.TodayRevenue = await ordersQuery
            .Where(o => o.CreatedAt >= today && o.Status == "Completed")
            .SumAsync(o => o.TotalAmount);

        ViewBag.MonthOrders = await ordersQuery
            .Where(o => o.CreatedAt >= startOfMonth && o.Status == "Completed")
            .CountAsync();

        ViewBag.MonthRevenue = await ordersQuery
            .Where(o => o.CreatedAt >= startOfMonth && o.Status == "Completed")
            .SumAsync(o => o.TotalAmount);

        var storesQuery = _context.Stores.AsQueryable();
        var productsQuery = _context.Products.AsQueryable();

        if (allowedStoreIds != null)
        {
            storesQuery = storesQuery.Where(s => allowedStoreIds.Contains(s.Id));
            productsQuery = productsQuery.Where(p => p.StoreId == null || (p.StoreId.HasValue && allowedStoreIds.Contains(p.StoreId.Value)));
        }

        if (storeId.HasValue)
        {
            storesQuery = storesQuery.Where(s => s.Id == storeId);
            
            productsQuery = productsQuery.Where(p => p.StoreId == storeId);
        }

        ViewBag.TotalStores = await storesQuery.CountAsync();
        ViewBag.TotalProducts = await productsQuery.CountAsync();
        ViewBag.LowStockProducts = await productsQuery.Where(p => p.StockQuantity <= 10).CountAsync();

        ViewBag.StoreId = storeId;
        ViewBag.Stores = await GetStoreSelectListAsync();

        return View();
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Sales(int? storeId, DateTime? fromDate, DateTime? toDate)
    {
        await SetPermissionViewBagAsync(_menuId);

        var from = fromDate ?? DateTime.Today.AddDays(-30);
        var to = toDate ?? DateTime.Today;

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var allowedStoreIds = isAdmin ? null : await GetAllowedStoreIdsAsync(_context);

        var query = _context.Orders
            .Include(o => o.Store)
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to.AddDays(1) && o.Status == "Completed");

        if (allowedStoreIds != null)
        {
            query = query.Where(o => allowedStoreIds.Contains(o.StoreId));
        }

        if (storeId.HasValue)
            query = query.Where(o => o.StoreId == storeId);

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
        var allowedStoreIds = isAdmin ? null : await GetAllowedStoreIdsAsync(_context);

        var query = _context.Products
            .Include(p => p.Store)
            .Include(p => p.ProductStores)
            .AsQueryable();

        if (allowedStoreIds != null)
        {
            query = query.Where(p => p.StoreId == null || (p.StoreId.HasValue && allowedStoreIds.Contains(p.StoreId.Value)));
        }

        if (storeId.HasValue)
            query = query.Where(p => p.StoreId == storeId || p.StoreId == null);

        var products = await query.ToListAsync();

        foreach (var p in products)
        {
            if (storeId.HasValue)
            {
                var storeStock = p.ProductStores.FirstOrDefault(ps => ps.StoreId == storeId);
                p.StockQuantity = storeStock?.Quantity ?? 0;
            }
            else
            {
                // Sum all stores (or allowed stores if restricted)
                if (allowedStoreIds != null)
                {
                    p.StockQuantity = p.ProductStores.Where(ps => allowedStoreIds.Contains(ps.StoreId)).Sum(ps => ps.Quantity);
                }
                else
                {
                    p.StockQuantity = p.ProductStores.Sum(ps => ps.Quantity);
                }
            }
        }

        products = products.OrderBy(p => p.StockQuantity).ToList();

        ViewBag.StoreId = storeId;
        ViewBag.Stores = await GetStoreSelectListAsync();

        return View(products);
    }



    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Shifts(int? storeId, DateTime? fromDate, DateTime? toDate)
    {
        await SetPermissionViewBagAsync(_menuId);

        var from = fromDate ?? DateTime.Today.AddDays(-7);
        var to = toDate ?? DateTime.Today;

        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var allowedStoreIds = isAdmin ? null : await GetAllowedStoreIdsAsync(_context);

        var query = _context.Shifts
            .Include(s => s.Store)
            .Include(s => s.Staff)
            .Where(s => s.StartTime >= from && s.StartTime <= to.AddDays(1));

        if (allowedStoreIds != null)
        {
            query = query.Where(s => allowedStoreIds.Contains(s.StoreId));
        }

        if (storeId.HasValue)
            query = query.Where(s => s.StoreId == storeId);

        var shifts = await query
            .OrderByDescending(s => s.StartTime)
            .ToListAsync();

        ViewBag.FromDate = from;
        ViewBag.ToDate = to;
        ViewBag.StoreId = storeId;
        ViewBag.Stores = await GetStoreSelectListAsync();

        return View(shifts);
    }

    private async Task<List<SelectListItem>> GetStoreSelectListAsync()
    {
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        var allowedStoreIds = isAdmin ? null : await GetAllowedStoreIdsAsync(_context);
        var query = _context.Stores.Where(s => s.IsActive);

        if (allowedStoreIds != null)
        {
            query = query.Where(s => allowedStoreIds.Contains(s.Id));
        }

        return await query
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
    }
}
