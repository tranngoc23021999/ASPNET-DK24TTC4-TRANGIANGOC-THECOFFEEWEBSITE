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
            var ownedStoreIds = await _context.Stores
                .Where(s => s.OwnerId == CurrentUserId)
                .Select(s => s.Id)
                .ToListAsync();

            var allowedStoreIds = userStoreIds.Union(ownedStoreIds).ToList();
            query = query.Where(p => allowedStoreIds.Contains(p.StoreId));
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
            StoreId = product.StoreId
        };

        ViewBag.Stores = await GetStoreSelectListAsync();
        ViewBag.Categories = GetCategorySelectList();
        return View(model);
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
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

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
            var userStoreIds = await _context.UserStores
                .Where(us => us.UserId == CurrentUserId)
                .Select(us => us.StoreId)
                .ToListAsync();

            var ownedStoreIds = await _context.Stores
                .Where(s => s.OwnerId == CurrentUserId)
                .Select(s => s.Id)
                .ToListAsync();

            var allowedStoreIds = userStoreIds.Union(ownedStoreIds).ToList();
            query = query.Where(s => allowedStoreIds.Contains(s.Id));
        }

        return await query
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
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
