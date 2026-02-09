using CoffeSolution.Data;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.ViewComponents;

/// <summary>
/// ViewComponent hiển thị Store Selector trên Navbar
/// - Administrator: Ẩn (xem tất cả)
/// - User có 1 store: Ẩn (auto select)
/// - User có nhiều stores: Hiển thị dropdown
/// </summary>
public class StoreSelectorViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public StoreSelectorViewComponent(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
        {
            return Content(string.Empty);
        }

        // Administrator không cần Store Selector
        var isAdministrator = await _permissionService.IsAdministratorAsync(userId.Value);
        if (isAdministrator)
        {
            return Content(string.Empty);
        }

        // Lấy danh sách stores: assigned + owned
        var assignedStores = await _context.UserStores
            .Include(us => us.Store)
            .Where(us => us.UserId == userId && us.Store.IsActive)
            .Select(us => new { Store = us.Store, IsDefault = us.IsDefault })
            .ToListAsync();
        
        var ownedStores = await _context.Stores
            .Where(s => s.OwnerId == userId && s.IsActive)
            .Select(s => new { Store = s, IsDefault = false })
            .ToListAsync();
        
        // Merge và loại bỏ trùng lặp
        var allStores = assignedStores
            .Concat(ownedStores)
            .GroupBy(x => x.Store.Id)
            .Select(g => g.First())
            .OrderBy(x => x.Store.Name)
            .ToList();

        // Nếu có <= 1 store thì ẩn selector
        if (allStores.Count <= 1)
        {
            return Content(string.Empty);
        }

        // Lấy store đang chọn trong session
        var currentStoreId = HttpContext.Session.GetInt32("CurrentStoreId");
        var currentStoreName = HttpContext.Session.GetString("CurrentStoreName") ?? "Chọn cửa hàng";

        var viewModel = new StoreSelectorViewModel
        {
            Stores = allStores.Select(x => new StoreOptionItem
            {
                Id = x.Store.Id,
                Name = x.Store.Name,
                Code = x.Store.Code,
                IsDefault = x.IsDefault
            }).ToList(),
            CurrentStoreId = currentStoreId,
            CurrentStoreName = currentStoreName
        };

        return View(viewModel);
    }
}
