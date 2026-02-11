using CoffeSolution.Attributes;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class ProductApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public ProductApiController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm theo Store
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts(int storeId, string? category = null, bool activeOnly = true)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.Error("Chưa đăng nhập"));

        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, "PRODUCT", "VIEW");
        if (!hasPermission)
            return Forbid();

        // Lấy danh sách Store mà user có quyền truy cập (Là Owner hoặc Staff)
        var userStoreIds = await _context.UserStores
            .Where(us => us.UserId == userId)
            .Select(us => us.StoreId)
            .ToListAsync();

        var ownedStoreIds = await _context.Stores
            .Where(s => s.OwnerId == userId)
            .Select(s => s.Id)
            .ToListAsync();
        
        var allowedStoreIds = userStoreIds.Union(ownedStoreIds).ToList();

        // Lấy danh sách Owner của các store này để check hàng Global
        var ownerIds = await _context.Stores
            .Where(s => allowedStoreIds.Contains(s.Id))
            .Select(s => s.OwnerId)
            .Distinct()
            .ToListAsync();
        
        // Nếu user là Admin/Owner thì thêm chính mình vào logic check
        if (!ownerIds.Contains(userId.Value)) ownerIds.Add(userId.Value);

        var query = _context.Products
            .Where(p => 
                p.StoreId == storeId || 
                (p.StoreId != null && allowedStoreIds.Contains(p.StoreId.Value)) ||
                (p.StoreId == null && p.CreatedById != null && ownerIds.Contains(p.CreatedById.Value)) ||
                (p.IsSystem == true && p.StoreId == null)
            );

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        var products = await query
            .Include(p => p.Store) // Join with Store
            .Include(p => p.ProductStores)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImageUrl,
                p.Category,
                StockQuantity = p.ProductStores
                    .Where(ps => ps.StoreId == (p.StoreId ?? storeId))
                    .Select(ps => ps.Quantity)
                    .FirstOrDefault(),
                p.Unit,
                p.IsActive,
                p.AllowNegativeStock,
                p.StoreId,
                StoreName = p.Store != null ? p.Store.Name : "Global" // Return Store Name or "Global"
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(products));
    }

    /// <summary>
    /// Lấy danh sách danh mục
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(int storeId)
    {
        var userId = _authService.GetCurrentUserId();
        var allowedStoreIds = new List<int>();

        if (userId != null)
        {
            var userStoreIds = await _context.UserStores
                .Where(us => us.UserId == userId)
                .Select(us => us.StoreId)
                .ToListAsync();

            var ownedStoreIds = await _context.Stores
                .Where(s => s.OwnerId == userId)
                .Select(s => s.Id)
                .ToListAsync();
            
            allowedStoreIds = userStoreIds.Union(ownedStoreIds).ToList();
        }

        // Lấy danh sách Owner của các store này để check hàng Global
        var ownerIds = await _context.Stores
            .Where(s => allowedStoreIds.Contains(s.Id))
            .Select(s => s.OwnerId)
            .Distinct()
            .ToListAsync();
        
        if (userId != null && !ownerIds.Contains(userId.Value)) ownerIds.Add(userId.Value);

        var categories = await _context.Products
            .Where(p => 
                (p.StoreId == storeId || 
                (p.StoreId != null && allowedStoreIds.Contains(p.StoreId.Value)) ||
                (p.StoreId == null && p.CreatedById != null && ownerIds.Contains(p.CreatedById.Value))) &&
                p.IsActive && p.Category != null)
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(categories));
    }
}
