using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

/// <summary>
/// Controller quản lý Roles và Permissions
/// Chỉ Administrator mới có quyền quản lý roles
/// </summary>
public class RoleController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Role;
    public RoleController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index()
    {
        await SetPermissionViewBagAsync(_menuId);

        var roles = await _context.Roles
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Id)
            .Select(r => new RoleListViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystem = r.IsSystem,
                UserCount = r.UserRoles.Count,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return View(roles);
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Details(int id)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .Include(r => r.RoleMenuPermissions)
                .ThenInclude(rmp => rmp.Menu)
            .Include(r => r.RoleMenuPermissions)
                .ThenInclude(rmp => rmp.MenuAction)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        await SetPermissionViewBagAsync(_menuId);
        return View(role);
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Menus = await GetMenusWithActionsAsync();
        return View(new RoleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (await _context.Roles.AnyAsync(r => r.Name == model.Name))
        {
            ModelState.AddModelError("Name", "Tên vai trò đã tồn tại");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Menus = await GetMenusWithActionsAsync();
            return View(model);
        }

        var role = new Role
        {
            Name = model.Name,
            Description = model.Description,
            IsSystem = false
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        // Gắn Permissions
        await SavePermissionsAsync(role.Id, model.Permissions);

        TempData[TempDataKey.Success] = Messages.CreateSuccess;
        return RedirectToAction(nameof(Index));
    }

    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var role = await _context.Roles
            .Include(r => r.RoleMenuPermissions)
                .ThenInclude(rmp => rmp.Menu)
            .Include(r => r.RoleMenuPermissions)
                .ThenInclude(rmp => rmp.MenuAction)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        // Không cho sửa role system
        if (role.IsSystem)
        {
            TempData[TempDataKey.Error] = "Không thể sửa vai trò hệ thống!";
            return RedirectToAction(nameof(Index));
        }

        var model = new RoleViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = role.RoleMenuPermissions
                .GroupBy(rmp => rmp.Menu.Code)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(rmp => rmp.MenuAction.Code).ToList())
        };

        ViewBag.Menus = await GetMenusWithActionsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, RoleViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Menus = await GetMenusWithActionsAsync();
            return View(model);
        }

        var role = await _context.Roles
            .Include(r => r.RoleMenuPermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        if (role.IsSystem)
        {
            TempData[TempDataKey.Error] = "Không thể sửa vai trò hệ thống!";
            return RedirectToAction(nameof(Index));
        }

        role.Name = model.Name;
        role.Description = model.Description;

        // Xóa permissions cũ
        _context.RoleMenuPermissions.RemoveRange(role.RoleMenuPermissions);
        await _context.SaveChangesAsync();

        // Gắn permissions mới
        await SavePermissionsAsync(role.Id, model.Permissions);

        TempData[TempDataKey.Success] = Messages.UpdateSuccess;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RoleMenuPermissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role == null) return NotFound();

        if (role.IsSystem)
        {
            TempData[TempDataKey.Error] = "Không thể xóa vai trò hệ thống!";
            return RedirectToAction(nameof(Index));
        }

        if (role.UserRoles.Any())
        {
            TempData[TempDataKey.Error] = "Không thể xóa vai trò đang được sử dụng!";
            return RedirectToAction(nameof(Index));
        }

        _context.RoleMenuPermissions.RemoveRange(role.RoleMenuPermissions);
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = Messages.DeleteSuccess;
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<Menu>> GetMenusWithActionsAsync()
    {
        return await _context.Menus
            .Include(m => m.MenuActions)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Order)
            .ToListAsync();
    }

    private async Task SavePermissionsAsync(int roleId, Dictionary<string, List<string>> permissions)
    {
        foreach (var (menuCode, actionCodes) in permissions)
        {
            var menu = await _context.Menus
                .Include(m => m.MenuActions)
                .FirstOrDefaultAsync(m => m.Code == menuCode);

            if (menu == null) continue;

            foreach (var actionCode in actionCodes)
            {
                var action = menu.MenuActions.FirstOrDefault(a => a.Code == actionCode);
                if (action == null) continue;

                _context.RoleMenuPermissions.Add(new RoleMenuPermission
                {
                    RoleId = roleId,
                    MenuId = menu.Id,
                    MenuActionId = action.Id,
                    GrantedAt = DateTime.Now
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}
