using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class OrderApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public OrderApiController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Tạo đơn hàng mới (POS)
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.Error("Chưa đăng nhập"));

        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, "ORDER", "CREATE");
        if (!hasPermission)
            return Forbid();

        if (request.Items == null || !request.Items.Any())
            return BadRequest(ApiResponse.Error("Vui lòng thêm sản phẩm vào đơn hàng"));

        // Tạo Order
        var order = new Order
        {
            OrderCode = $"ORD-{DateTime.Now:yyyyMMdd-HHmmss}",
            StoreId = request.StoreId,
            StaffId = userId,
            Status = "Completed",
            PaymentMethod = request.PaymentMethod ?? "Cash",
            Note = request.Note,
            CreatedAt = DateTime.Now
        };

        decimal totalAmount = 0;

        // Thêm OrderDetails
        foreach (var item in request.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null)
                return BadRequest(ApiResponse.Error($"Không tìm thấy sản phẩm ID: {item.ProductId}"));

            var detail = new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                Amount = product.Price * item.Quantity,
                Note = item.Note
            };

            totalAmount += detail.Amount;
            order.OrderDetails.Add(detail);

            // Giảm tồn kho (ProductStore)
            var productStore = await _context.ProductStores
                .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.StoreId == request.StoreId);

            if (productStore == null)
            {
                // Nếu chưa có record tồn kho cho store này thì tạo mới
                productStore = new ProductStore
                {
                    ProductId = item.ProductId,
                    StoreId = request.StoreId,
                    Quantity = 0, // Mặc định 0
                    LastUpdated = DateTime.Now
                };
                _context.ProductStores.Add(productStore);
            }

            if (product.AllowNegativeStock)
            {
                 productStore.Quantity -= item.Quantity;
            }
            else
            {
                 if (productStore.Quantity < item.Quantity)
                 {
                     return BadRequest(ApiResponse.Error($"Sản phẩm {product.Name} không đủ tồn kho (Còn: {productStore.Quantity})"));
                 }
                 productStore.Quantity -= item.Quantity;
            }
            productStore.LastUpdated = DateTime.Now;
        }

        order.TotalAmount = totalAmount;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(new OrderPosViewModel
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt
        }, "Tạo đơn hàng thành công!"));
    }

    /// <summary>
    /// Lấy đơn hàng theo ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.Error("Chưa đăng nhập"));

        var order = await _context.Orders
            .Include(o => o.Store)
            .Include(o => o.Staff)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(ApiResponse.Error("Không tìm thấy đơn hàng"));

        var result = new
        {
            order.Id,
            order.OrderCode,
            order.CreatedAt,
            order.TotalAmount,
            order.Status,
            order.PaymentMethod,
            order.Note,
            StoreName = order.Store.Name,
            StaffName = order.Staff?.FullName,
            Items = order.OrderDetails.Select(od => new
            {
                od.ProductId,
                ProductName = od.Product.Name,
                od.Quantity,
                od.UnitPrice,
                od.Amount,
                od.Note
            })
        };

        return Ok(ApiResponse<object>.Ok(result));
    }

    /// <summary>
    /// Cập nhật trạng thái đơn hàng
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.Error("Chưa đăng nhập"));

        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, "ORDER", "EDIT");
        if (!hasPermission)
            return Forbid();

        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound(ApiResponse.Error("Không tìm thấy đơn hàng"));

        order.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Cập nhật trạng thái thành công"));
    }
    /// <summary>
    /// Lấy danh sách đơn hàng gần đây của nhân viên tại cửa hàng
    /// </summary>
    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentOrders(int storeId)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse.Error("Chưa đăng nhập"));

        var orders = await _context.Orders
            .Where(o => o.StoreId == storeId && o.StaffId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .Select(o => new
            {
                o.Id,
                o.OrderCode,
                o.TotalAmount,
                o.Status,
                o.CreatedAt,
                o.PaymentMethod
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(orders));
    }
}

public class CreateOrderRequest
{
    public int StoreId { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public string? Note { get; set; }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = null!;
}
