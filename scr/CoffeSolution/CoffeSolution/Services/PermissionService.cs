using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Services;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(int userId, string menuCode, string actionCode);
    Task<List<Menu>> GetUserMenusAsync(int userId);
    Task<List<string>> GetUserActionsAsync(int userId, string menuCode);
    Task<bool> IsAdministratorAsync(int userId);
}

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _context;

    public PermissionService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Kiểm tra user có quyền thực hiện action trên menu không
    /// </summary>
    public async Task<bool> HasPermissionAsync(int userId, string menuCode, string actionCode)
    {
        // Lấy tất cả roleIds của user
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any()) return false;

        // Kiểm tra có role Administrator không (full access)
        var isAdmin = await _context.Roles
            .AnyAsync(r => roleIds.Contains(r.Id) && r.Name == "Administrator");

        if (isAdmin) return true;

        // Kiểm tra permission cụ thể
        return await _context.RoleMenuPermissions
            .Include(rmp => rmp.Menu)
            .Include(rmp => rmp.MenuAction)
            .AnyAsync(rmp =>
                roleIds.Contains(rmp.RoleId) &&
                rmp.Menu.Code == menuCode &&
                rmp.MenuAction.Code == actionCode);
    }

    /// <summary>
    /// Lấy danh sách menu mà user có quyền xem
    /// </summary>
    public async Task<List<Menu>> GetUserMenusAsync(int userId)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any()) return new List<Menu>();

        // Administrator thấy tất cả menu
        var isAdmin = await _context.Roles
            .AnyAsync(r => roleIds.Contains(r.Id) && r.Name == "Administrator");

        if (isAdmin)
        {
            return await _context.Menus
                .Where(m => m.IsActive)
                .OrderBy(m => m.Order)
                .ToListAsync();
        }

        // Lấy menu có quyền VIEW
        var menuIds = await _context.RoleMenuPermissions
            .Include(rmp => rmp.MenuAction)
            .Where(rmp => roleIds.Contains(rmp.RoleId) && rmp.MenuAction.Code == "VIEW")
            .Select(rmp => rmp.MenuId)
            .Distinct()
            .ToListAsync();

        return await _context.Menus
            .Where(m => menuIds.Contains(m.Id) && m.IsActive)
            .OrderBy(m => m.Order)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy danh sách actions mà user có quyền trên menu
    /// </summary>
    public async Task<List<string>> GetUserActionsAsync(int userId, string menuCode)
    {
        var roleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        if (!roleIds.Any()) return new List<string>();

        // Administrator có tất cả quyền
        var isAdmin = await _context.Roles
            .AnyAsync(r => roleIds.Contains(r.Id) && r.Name == "Administrator");

        if (isAdmin)
        {
            return await _context.MenuActions
                .Include(ma => ma.Menu)
                .Where(ma => ma.Menu.Code == menuCode)
                .Select(ma => ma.Code)
                .ToListAsync();
        }

        return await _context.RoleMenuPermissions
            .Include(rmp => rmp.Menu)
            .Include(rmp => rmp.MenuAction)
            .Where(rmp =>
                roleIds.Contains(rmp.RoleId) &&
                rmp.Menu.Code == menuCode)
            .Select(rmp => rmp.MenuAction.Code)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Kiểm tra user có phải Administrator không
    /// </summary>
    public async Task<bool> IsAdministratorAsync(int userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == "Administrator");
    }
}
