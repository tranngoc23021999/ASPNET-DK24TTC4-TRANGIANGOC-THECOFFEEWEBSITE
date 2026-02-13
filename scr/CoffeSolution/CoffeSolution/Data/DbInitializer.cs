using Microsoft.EntityFrameworkCore;
using CoffeSolution.Models.Entities;
using CoffeSolution.Constants;

namespace CoffeSolution.Data;

/// <summary>
/// Tự động seed dữ liệu bắt buộc khi chạy migration
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            // Đảm bảo database đã được tạo và apply migrations
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed.");

            // Seed theo thứ tự
            await SeedRolesAsync(context, logger);
            await SeedMenusAsync(context, logger);
            await SeedMenuActionsAsync(context, logger);
            await SeedRolePermissionsAsync(context, logger);
            await SeedAdminUserAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    /// <summary>
    /// Seed 4 Roles mặc định
    /// </summary>
    private static async Task SeedRolesAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Roles.AnyAsync())
        {
            logger.LogInformation("Roles already seeded.");
            return;
        }

        var roles = new List<Role>
        {
            new() { Name = "Administrator", Description = "Quản trị hệ thống - toàn quyền", IsSystem = true },
            new() { Name = "Admin", Description = "Chủ cửa hàng - quản lý cửa hàng của mình", IsSystem = true },
            new() { Name = "Leader", Description = "Quản lý ca/nhóm - giám sát nhân viên", IsSystem = true },
            new() { Name = "Staff", Description = "Nhân viên - bán hàng", IsSystem = true }
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} roles.", roles.Count);
    }

    /// <summary>
    /// Seed Menus hệ thống
    /// </summary>
    private static async Task SeedMenusAsync(ApplicationDbContext context, ILogger logger)
    {
        var menus = new List<Menu>
        {
            new() { Code = MenuCode.Dashboard, Name = "Dashboard", Url = "/", Icon = "fa-dashboard", Order = 1 },
            new() { Code = MenuCode.Store, Name = "Cửa hàng", Url = "/Store", Icon = "fa-store", Order = 2 },
            new() { Code = MenuCode.Product, Name = "Sản phẩm", Url = "/Product", Icon = "fa-coffee", Order = 3 },
            new() { Code = MenuCode.Order, Name = "Đơn hàng", Url = "/Order", Icon = "fa-shopping-cart", Order = 4 },
            new() { Code = MenuCode.POS, Name = "Bán hàng", Url = "/POS", Icon = "fa-cash-register", Order = 5 },
            new() { Code = MenuCode.Warehouse, Name = "Kho hàng", Url = "/Warehouse", Icon = "fa-warehouse", Order = 6 },
            new() { Code = MenuCode.Supplier, Name = "Nhà cung cấp", Url = "/Supplier", Icon = "fa-truck", Order = 7 },
            new() { Code = MenuCode.Customer, Name = "Khách hàng", Url = "/Customer", Icon = "fa-users", Order = 8 },
            new() { Code = MenuCode.Employee, Name = "Nhân viên", Url = "/Employee", Icon = "fa-user-tie", Order = 9 },
            new() { Code = MenuCode.Report, Name = "Báo cáo", Url = "/Report", Icon = "fa-chart-bar", Order = 10 },
            new() { Code = MenuCode.User, Name = "Người dùng", Url = "/User", Icon = "fa-user-cog", Order = 11 },
            new() { Code = MenuCode.Role, Name = "Phân quyền", Url = "/Role", Icon = "fa-shield-alt", Order = 12 },
            new() { Code = MenuCode.Setting, Name = "Cài đặt", Url = "/Setting", Icon = "fa-cog", Order = 13 },
            new() { Code = MenuCode.Shift, Name = "Phiên làm việc", Url = "/Shift", Icon = "fa-clock", Order = 5 }
        };

        foreach (var menu in menus)
        {
            if (!await context.Menus.AnyAsync(m => m.Code == menu.Code))
            {
                context.Menus.Add(menu);
                logger.LogInformation($"Seeded new menu: {menu.Name}");
            }
        }
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seed MenuActions cho mỗi Menu
    /// </summary>
    private static async Task SeedMenuActionsAsync(ApplicationDbContext context, ILogger logger)
    {
        var menus = await context.Menus.ToListAsync();
        var standardActions = new[]
        {
            (ActionCode.View, "Xem", 1),
            (ActionCode.Create, "Thêm", 2),
            (ActionCode.Edit, "Sửa", 3),
            (ActionCode.Delete, "Xóa", 4),
            (ActionCode.Export, "Xuất file", 5)
        };

        var actionsToAdd = new List<MenuAction>();

        foreach (var menu in menus)
        {
            foreach (var (code, name, order) in standardActions)
            {
                if (!await context.MenuActions.AnyAsync(a => a.MenuId == menu.Id && a.Code == code))
                {
                    actionsToAdd.Add(new MenuAction
                    {
                        MenuId = menu.Id,
                        Code = code,
                        Name = name,
                        Order = order
                    });
                }
            }
        }

        if (actionsToAdd.Any())
        {
            context.MenuActions.AddRange(actionsToAdd);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} new menu actions.", actionsToAdd.Count);
        }
    }

    /// <summary>
    /// Seed RoleMenuPermissions - gắn quyền cho từng Role
    /// </summary>
    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Removed early return to allow updating permissions
        // if (await context.RoleMenuPermissions.AnyAsync()) ...

        var roles = await context.Roles.ToListAsync();
        var menus = await context.Menus.Include(m => m.MenuActions).ToListAsync();

        var adminRole = roles.First(r => r.Name == "Administrator");
        var ownerRole = roles.First(r => r.Name == "Admin");
        var leaderRole = roles.First(r => r.Name == "Leader");
        var staffRole = roles.First(r => r.Name == "Staff");

        var permissions = new List<RoleMenuPermission>();

        // Administrator: Full access tất cả
        foreach (var menu in menus)
        {
            foreach (var action in menu.MenuActions)
            {
                permissions.Add(new RoleMenuPermission
                {
                    RoleId = adminRole.Id,
                    MenuId = menu.Id,
                    MenuActionId = action.Id
                });
            }
        }

        // Admin (Chủ cửa hàng): Full access trừ ROLE, SETTING
        var adminExcludeMenus = new[] { "ROLE", "SETTING" };
        foreach (var menu in menus.Where(m => !adminExcludeMenus.Contains(m.Code)))
        {
            foreach (var action in menu.MenuActions)
            {
                permissions.Add(new RoleMenuPermission
                {
                    RoleId = ownerRole.Id,
                    MenuId = menu.Id,
                    MenuActionId = action.Id
                });
            }
        }

        // Leader: VIEW/CREATE/EDIT cho ORDER, POS, PRODUCT, EMPLOYEE + VIEW cho các menu khác
        var leaderFullMenus = new[] { "ORDER", "POS", "PRODUCT", "EMPLOYEE", "DASHBOARD", "SHIFT" };
        var leaderViewMenus = new[] { "WAREHOUSE", "CUSTOMER", "REPORT" };
        
        foreach (var menu in menus.Where(m => leaderFullMenus.Contains(m.Code)))
        {
            foreach (var action in menu.MenuActions.Where(a => a.Code != "DELETE"))
            {
                permissions.Add(new RoleMenuPermission
                {
                    RoleId = leaderRole.Id,
                    MenuId = menu.Id,
                    MenuActionId = action.Id
                });
            }
        }
        
        foreach (var menu in menus.Where(m => leaderViewMenus.Contains(m.Code)))
        {
            var viewAction = menu.MenuActions.FirstOrDefault(a => a.Code == "VIEW");
            if (viewAction != null)
            {
                permissions.Add(new RoleMenuPermission
                {
                    RoleId = leaderRole.Id,
                    MenuId = menu.Id,
                    MenuActionId = viewAction.Id
                });
            }
        }

        // Staff: VIEW/CREATE cho ORDER, POS + VIEW cho PRODUCT, DASHBOARD
        var staffFullMenus = new[] { "ORDER", "POS" };
        var staffViewMenus = new[] { "PRODUCT", "DASHBOARD" };
        
        foreach (var menu in menus.Where(m => staffFullMenus.Contains(m.Code)))
        {
            foreach (var action in menu.MenuActions.Where(a => a.Code == "VIEW" || a.Code == "CREATE"))
            {
                permissions.Add(new RoleMenuPermission
                {
                    RoleId = staffRole.Id,
                    MenuId = menu.Id,
                    MenuActionId = action.Id
                });
            }
        }
        
        foreach (var menu in menus.Where(m => staffViewMenus.Contains(m.Code)))
        {
            var viewAction = menu.MenuActions.FirstOrDefault(a => a.Code == "VIEW");
            if (viewAction != null)
            {
                permissions.Add(new RoleMenuPermission
                {
                    RoleId = staffRole.Id,
                    MenuId = menu.Id,
                    MenuActionId = viewAction.Id
                });
            }
        }

        foreach (var perm in permissions)
        {
            if (!await context.RoleMenuPermissions.AnyAsync(p => p.RoleId == perm.RoleId && p.MenuId == perm.MenuId && p.MenuActionId == perm.MenuActionId))
            {
                context.RoleMenuPermissions.Add(perm);
            }
        }
        
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded role permissions (if missing).");
    }
    


    /// <summary>
    /// Seed Admin User mặc định
    /// </summary>
    private static async Task SeedAdminUserAsync(ApplicationDbContext context, ILogger logger)
    {
        var adminRoleId = await context.Roles
            .Where(r => r.Name == "Administrator")
            .Select(r => r.Id)
            .FirstAsync();

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "trangiangoc");
        if (adminUser == null)
        {
            adminUser = new User
            {
                Username = "trangiangoc",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("trangiangoc@123"),
                FullName = "Trần Gia Ngọc",
                Email = "TranGiaNgoc@coffeeshop.com",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded admin user: trangiangoc/trangiangoc@123");
        }

        // Ensure Admin has Administrator role
        var hasRole = await context.UserRoles
            .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRoleId);

        if (!hasRole)
        {
            context.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRoleId
            });
            await context.SaveChangesAsync();
            logger.LogInformation("Assigned Administrator role to admin user.");
        }

        logger.LogInformation("Seeded admin user: trangiangoc/trangiangoc@123");
    }
}
