using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class CustomerApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public CustomerApiController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Tìm kiếm khách hàng (POS)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(string query)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.Error("Chưa đăng nhập"));

        if (string.IsNullOrEmpty(query))
            return Ok(ApiResponse<List<object>>.Ok(new List<object>()));

        // Logic filter tương tự CustomerController
        // Nhưng ở POS ta chỉ cần tìm khách hàng có thể bán được (thuộc store hiện tại hoặc global)
        
        var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
        
        // TODO: Cần check permission nếu cần
        
        var customers = await _context.Customers
            .AsNoTracking()
            .Where(c => 
                (c.Phone.Contains(query) || c.Name.Contains(query) || c.Code.Contains(query)) &&
                (c.StoreId == null || c.StoreId == currentStoreId) // Global OR Current Store
            )
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.Name,
                c.Phone,
                c.Address,
                c.Note
            })
            .Take(10) // Limit results
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(customers));
    }
}
