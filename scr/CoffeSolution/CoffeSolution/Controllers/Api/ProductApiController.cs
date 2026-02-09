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

        var query = _context.Products
            .Where(p => p.StoreId == storeId);

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        var products = await query
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
                p.StockQuantity,
                p.Unit,
                p.IsActive
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
        var categories = await _context.Products
            .Where(p => p.StoreId == storeId && p.IsActive && p.Category != null)
            .Select(p => p.Category)
            .Distinct()
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(categories));
    }
}
