using CoffeSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CoffeSolution.Attributes;

/// <summary>
/// Attribute kiểm tra quyền truy cập
/// Sử dụng: [Permission("ORDER", "CREATE")]
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class PermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string MenuCode { get; }
    public string ActionCode { get; }

    public PermissionAttribute(string menuCode, string actionCode)
    {
        MenuCode = menuCode;
        ActionCode = actionCode;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Kiểm tra đã đăng nhập chưa
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // Lấy PermissionService từ DI
        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();

        var userId = authService.GetCurrentUserId();
        if (userId == null)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // Kiểm tra quyền
        var hasPermission = await permissionService.HasPermissionAsync(userId.Value, MenuCode, ActionCode);
        if (!hasPermission)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
        }
    }
}
