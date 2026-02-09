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

public class ProductController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Product;

    public ProductController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index(string? search, int? storeId, int page = 1)
    {
        await SetPermissionViewBagAsync(_menuId);

        var query = _context.Products
            .Include(p => p.Store)
            .AsQueryable();

        // Lọc theo store mà user có quyền
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        if (!isAdmin)
        {
            var userStoreIds = await _context.UserStores
                .Where(us => us.UserId == CurrentUserId)
                .Select(us => us.StoreId)
                .ToListAsync();

            // Thêm store mà user sở hữu
            var ownedStores = await _context.Stores
                .Where(s => s.OwnerId == CurrentUserId)
                .Select(s => new { s.Id, s.OwnerId })
                .ToListAsync();

            var ownedStoreIds = ownedStores.Select(s => s.Id).ToList();
            var allowedStoreIds = userStoreIds.Union(ownedStoreIds).Cast<int?>().ToList();
            
            // Get OwnerIds of the stores this user belongs to (for Owner-Global visibility)
            // If user is Staff in Store A (Owner X), user should see items CreatedBy X with StoreId=null.
            var ownerIds = await _context.Stores
                .Where(s => allowedStoreIds.Contains(s.Id))
                .Select(s => s.OwnerId)
                .Distinct()
                .ToListAsync();
            
            // Add user's own Id if they are an owner
             if (User.IsInRole("Admin")) 
             {
                 ownerIds.Add(CurrentUserId!.Value);
             }

            // Filter logic:
            // 1. IsSystem == true (Visible to all)
            // 2. StoreId in AllowedStores (Specific store items)
            // 3. StoreId == null AND CreatedById in ownerIds (Owner-Global items)
            // 4. StoreId == null AND CreatedById == CurrentUserId (My own global items)
            
            query = query.Where(p => 
                p.IsSystem == true || 
                allowedStoreIds.Contains(p.StoreId) ||
                (p.StoreId == null && p.CreatedById.HasValue && ownerIds.Contains(p.CreatedById.Value))
            );
        }

        if (storeId.HasValue)
        {
            query = query.Where(p => p.StoreId == storeId);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Category != null && p.Category.Contains(search)));
        }

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.StoreId = storeId;
        ViewBag.Stores = await GetStoreSelectListAsync();

        return View(products);
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Stores = await GetStoreSelectListAsync();
        ViewBag.Categories = GetCategorySelectList();
        return View(new ProductViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(ProductViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreSelectListAsync();
            ViewBag.Categories = GetCategorySelectList();
            return View(model);
        }

        // Validate Store Access
        var isAdmin = await PermissionService.IsAdministratorAsync(CurrentUserId!.Value);
        
        // Setup Product Scoping
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
                // Assuming Admin role name is "Admin"
                var isOwner = User.IsInRole("Admin");
                if (isOwner)
                {
                    // Owner -> Owner Global
                    model.IsSystem = false;
                    model.CreatedById = CurrentUserId;
                }
                else
                {
                    ModelState.AddModelError("StoreId", "Bạn không có quyền tạo sản phẩm toàn cục.");
                    ViewBag.Stores = await GetStoreSelectListAsync();
                    ViewBag.Categories = GetCategorySelectList();
                    return View(model);
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
                      ModelState.AddModelError("StoreId", "Bạn không có quyền tạo sản phẩm cho cửa hàng này.");
                      return View(model);
                 }
             }
        }

        var product = new Product
        {
            Name = model.Name,
            Description = model.Description,
            Price = model.Price,
            ImageUrl = model.ImageUrl,
            Category = model.Category,
            StockQuantity = model.StockQuantity,
            Unit = model.Unit,
            IsActive = model.IsActive,
            StoreId = model.StoreId,
            IsSystem = model.IsSystem,
            CreatedById = model.CreatedById ?? CurrentUserId, // Track creator always if possible
            CreatedAt = DateTime.Now
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Tạo sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        var model = new ProductViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            Category = product.Category,
            StockQuantity = product.StockQuantity,
            Unit = product.Unit,
            IsActive = product.IsActive,
            StoreId = product.StoreId,
            CreatedAt = product.CreatedAt
        };

        ViewBag.Stores = await GetStoreSelectListAsync();
        ViewBag.Categories = GetCategorySelectList();
        return View(model);
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Details(int id)
    {
        var product = await _context.Products
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, ProductViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreSelectListAsync();
            ViewBag.Categories = GetCategorySelectList();
            return View(model);
        }

        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.ImageUrl = model.ImageUrl;
        product.Category = model.Category;
        product.StockQuantity = model.StockQuantity;
        product.Unit = model.Unit;
        product.IsActive = model.IsActive;
        product.StoreId = model.StoreId;

        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Cập nhật sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.OrderDetails)
            .Include(p => p.WarehouseReceiptDetails)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // Check dependencies
        if (product.OrderDetails.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa sản phẩm đã phát sinh đơn hàng!";
            return RedirectToAction(nameof(Index));
        }

        if (product.WarehouseReceiptDetails.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa sản phẩm đã có phiếu nhập/xuất kho!";
            return RedirectToAction(nameof(Index));
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa sản phẩm thành công!";
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
            stores.Insert(0, new SelectListItem { Value = "", Text = "-- Hệ thống (Toàn cục) --" });
        }
        else if (User.IsInRole("Admin")) // Owner
        {
            stores.Insert(0, new SelectListItem { Value = "", Text = "-- Tất cả cửa hàng của tôi --" });
        }

        return stores;
    }

    private List<SelectListItem> GetCategorySelectList()
    {
        return new List<SelectListItem>
        {
            new("Cà phê", "Cà phê"),
            new("Trà", "Trà"),
            new("Sinh tố", "Sinh tố"),
            new("Nước ép", "Nước ép"),
            new("Đồ uống khác", "Đồ uống khác"),
            new("Bánh ngọt", "Bánh ngọt"),
            new("Đồ ăn nhẹ", "Đồ ăn nhẹ")
        };
    }
}
