using System.Diagnostics;
using CoffeSolution.Models;
using Microsoft.AspNetCore.Mvc;
using CoffeSolution.Services;
using CoffeSolution.Data;
using CoffeSolution.ViewModels;
using Microsoft.EntityFrameworkCore;
using CoffeSolution.Models.Entities;

namespace CoffeSolution.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IAuthService authService,
            IPermissionService permissionService)
            : base(authService, permissionService)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return View("Landing"); 
            }

            var userId = CurrentUserId;
            if (userId == null)
            {
                 return RedirectToAction("Login", "Auth");
            }

            // Dashboard cho user đã đăng nhập
            var isAdmin = await PermissionService.IsAdministratorAsync(userId.Value);
            
            List<int>? allowedStoreIds = null;
            if (!isAdmin)
            {
                allowedStoreIds = await GetAllowedStoreIdsAsync(_context);
            }

            // Dashboard data
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var ordersQuery = _context.Orders.AsQueryable();
            var storesQuery = _context.Stores.AsQueryable();
            var productsQuery = _context.Products.AsQueryable();

            if (allowedStoreIds != null)
            {
                ordersQuery = ordersQuery.Where(o => allowedStoreIds.Contains(o.StoreId));
                storesQuery = storesQuery.Where(s => allowedStoreIds.Contains(s.Id));
                productsQuery = productsQuery.Where(p => p.StoreId == null || (p.StoreId.HasValue && allowedStoreIds.Contains(p.StoreId.Value)));
            }

            // 1. Thống kê hôm nay
            var totalOrdersToday = await ordersQuery
                .Where(o => o.CreatedAt >= today && o.Status == "Completed")
                .CountAsync();

            var totalRevenueToday = await ordersQuery
                .Where(o => o.CreatedAt >= today && o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            // 2. Thống kê tổng quan
            var totalProducts = await productsQuery
                .CountAsync(p => p.IsActive);

            var activeStores = await storesQuery
                .CountAsync(s => s.IsActive);

            // 3. Đơn hàng gần đây (Top 5)
            var recentOrders = await ordersQuery
                .Include(o => o.Store) 
                .Include(o => o.Staff)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

             // 4. Sản phẩm bán chạy (Top 5 theo số lượng)
             var orderHeaders = ordersQuery.Where(o => o.Status == "Completed");
             
             var topProducts = await _context.OrderDetails
                .Where(od => orderHeaders.Select(o => o.Id).Contains(od.OrderId))
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Amount)
                })
                .OrderByDescending(kp => kp.QuantitySold)
                .Take(5)
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalOrdersToday = totalOrdersToday,
                TotalRevenueToday = totalRevenueToday,
                TotalProducts = totalProducts,
                ActiveStores = activeStores,
                RecentOrders = recentOrders,
                TopSellingProducts = topProducts
            };

            return View("Dashboard", viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
