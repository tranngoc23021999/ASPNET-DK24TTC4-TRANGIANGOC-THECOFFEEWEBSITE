using System.Security.Claims;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeSolution.Controllers;

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
}
