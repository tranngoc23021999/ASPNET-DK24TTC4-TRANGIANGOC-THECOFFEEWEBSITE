using System.Security.Claims;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

/// <summary>
/// Interface để đánh dấu entity có thuộc về Store
/// </summary>
public interface IStoreEntity
{
    int StoreId { get; set; }
}

/// <summary>
/// Base Controller chứa các property và method dùng chung
/// </summary>
[Authorize]
public abstract class BaseController : Controller
{
    protected readonly IAuthService AuthService;
    protected readonly IPermissionService PermissionService;

    protected BaseController(IAuthService authService, IPermissionService permissionService)
    {
        AuthService = authService;
        PermissionService = permissionService;
    }

    /// <summary>
    /// Id của user đang đăng nhập
    /// </summary>
    protected int? CurrentUserId => AuthService.GetCurrentUserId();

    /// <summary>
    /// Store Id đang được chọn (từ Session)
    /// </summary>
    protected int? CurrentStoreId => HttpContext.Session.GetInt32("CurrentStoreId");

    /// <summary>
    /// Kiểm tra user hiện tại có phải Administrator không
    /// </summary>
    protected async Task<bool> IsAdministratorAsync()
    {
        var userId = CurrentUserId;
        if (userId == null) return false;
        return await PermissionService.IsAdministratorAsync(userId.Value);
    }

    /// <summary>
    /// Lấy user đang đăng nhập (async)
    /// </summary>
    protected Task<User?> GetCurrentUserAsync() => AuthService.GetCurrentUserAsync();

    /// <summary>
    /// Kiểm tra user có quyền không
    /// </summary>
    protected async Task<bool> HasPermissionAsync(string menuCode, string actionCode)
    {
        var userId = CurrentUserId;
        if (userId == null) return false;
        return await PermissionService.HasPermissionAsync(userId.Value, menuCode, actionCode);
    }

    /// <summary>
    /// Lấy danh sách actions mà user có quyền trên menu hiện tại
    /// </summary>
    protected async Task<List<string>> GetCurrentActionsAsync(string menuCode)
    {
        var userId = CurrentUserId;
        if (userId == null) return new List<string>();
        return await PermissionService.GetUserActionsAsync(userId.Value, menuCode);
    }

    /// <summary>
    /// Set ViewBag với các actions để View biết nút nào được hiện
    /// </summary>
    protected async Task SetPermissionViewBagAsync(string menuCode)
    {
        var actions = await GetCurrentActionsAsync(menuCode);
        ViewBag.CanView = actions.Contains("VIEW");
        ViewBag.CanCreate = actions.Contains("CREATE");
        ViewBag.CanEdit = actions.Contains("EDIT");
        ViewBag.CanDelete = actions.Contains("DELETE");
        ViewBag.CanExport = actions.Contains("EXPORT");
    }

    /// <summary>
    /// Filter query theo Store dựa trên quyền của user
    /// - Administrator: Xem tất cả (không filter)
    /// - Có CurrentStoreId: Filter theo store đang chọn
    /// - Fallback: Filter theo tất cả stores của user
    /// </summary>
    protected async Task<IQueryable<T>> FilterByStoreAsync<T>(
        IQueryable<T> query,
        ApplicationDbContext context) where T : class, IStoreEntity
    {
        // Administrator: Xem tất cả
        if (await IsAdministratorAsync())
            return query;

        // Có CurrentStoreId: Filter theo store đang chọn
        if (CurrentStoreId.HasValue)
            return query.Where(x => x.StoreId == CurrentStoreId.Value);

        // Fallback: Filter theo tất cả stores của user
        var userId = CurrentUserId;
        if (userId == null)
            return query.Where(x => false); // Không có user -> không trả về gì

        var storeIds = await context.UserStores
            .Where(us => us.UserId == userId.Value)
            .Select(us => us.StoreId)
            .ToListAsync();

        return query.Where(x => storeIds.Contains(x.StoreId));
    }

    /// <summary>
    /// Lấy danh sách StoreId mà user có quyền truy cập
    /// </summary>
    protected async Task<List<int>> GetAllowedStoreIdsAsync(ApplicationDbContext context)
    {
        var userId = CurrentUserId;
        if (userId == null) return new List<int>();

        var userStoreIds = await context.UserStores
            .Where(us => us.UserId == userId.Value)
            .Select(us => us.StoreId)
            .ToListAsync();

        var ownedStoreIds = await context.Stores
            .Where(s => s.OwnerId == userId.Value)
            .Select(s => s.Id)
            .ToListAsync();

        return userStoreIds.Union(ownedStoreIds).ToList();
    }
}
