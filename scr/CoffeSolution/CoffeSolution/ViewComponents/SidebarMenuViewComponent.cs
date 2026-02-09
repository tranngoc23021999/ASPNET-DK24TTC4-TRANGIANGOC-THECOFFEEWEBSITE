using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.ViewComponents;

public class SidebarMenuViewComponent : ViewComponent
{
    private readonly IPermissionService _permissionService;
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public SidebarMenuViewComponent(
        IPermissionService permissionService,
        IAuthService authService,
        ApplicationDbContext context)
    {
        _permissionService = permissionService;
        _authService = authService;
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
        {
            return View(new List<Menu>());
        }

        var menus = await _permissionService.GetUserMenusAsync(userId.Value);
        return View(menus);
    }
}
