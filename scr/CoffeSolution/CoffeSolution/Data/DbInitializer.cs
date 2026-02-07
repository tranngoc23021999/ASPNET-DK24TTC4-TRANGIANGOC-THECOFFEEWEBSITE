using Microsoft.EntityFrameworkCore;
using CoffeSolution.Models.Entities;

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
        if (await context.Menus.AnyAsync())
        {
            logger.LogInformation("Menus already seeded.");
            return;
        }

        var menus = new List<Menu>
        {
            new() { Code = "DASHBOARD", Name = "Dashboard", Url = "/", Icon = "fa-dashboard", Order = 1 },
            new() { Code = "STORE", Name = "Cửa hàng", Url = "/Store", Icon = "fa-store", Order = 2 },
            new() { Code = "PRODUCT", Name = "Sản phẩm", Url = "/Product", Icon = "fa-coffee", Order = 3 },
            new() { Code = "ORDER", Name = "Đơn hàng", Url = "/Order", Icon = "fa-shopping-cart", Order = 4 },
            new() { Code = "POS", Name = "Bán hàng", Url = "/POS", Icon = "fa-cash-register", Order = 5 },
            new() { Code = "WAREHOUSE", Name = "Kho hàng", Url = "/Warehouse", Icon = "fa-warehouse", Order = 6 },
            new() { Code = "SUPPLIER", Name = "Nhà cung cấp", Url = "/Supplier", Icon = "fa-truck", Order = 7 },
            new() { Code = "CUSTOMER", Name = "Khách hàng", Url = "/Customer", Icon = "fa-users", Order = 8 },
            new() { Code = "EMPLOYEE", Name = "Nhân viên", Url = "/Employee", Icon = "fa-user-tie", Order = 9 },
            new() { Code = "REPORT", Name = "Báo cáo", Url = "/Report", Icon = "fa-chart-bar", Order = 10 },
            new() { Code = "USER", Name = "Người dùng", Url = "/User", Icon = "fa-user-cog", Order = 11 },
            new() { Code = "ROLE", Name = "Phân quyền", Url = "/Role", Icon = "fa-shield-alt", Order = 12 },
            new() { Code = "SETTING", Name = "Cài đặt", Url = "/Setting", Icon = "fa-cog", Order = 13 }
        };

        context.Menus.AddRange(menus);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} menus.", menus.Count);
    }

    /// <summary>
    /// Seed MenuActions cho mỗi Menu
    /// </summary>
    private static async Task SeedMenuActionsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.MenuActions.AnyAsync())
        {
            logger.LogInformation("MenuActions already seeded.");
            return;
        }

        var menus = await context.Menus.ToListAsync();
        var actions = new List<MenuAction>();

        // Actions chuẩn cho mỗi menu
        var standardActions = new[]
        {
            ("VIEW", "Xem", 1),
            ("CREATE", "Thêm", 2),
            ("EDIT", "Sửa", 3),
            ("DELETE", "Xóa", 4),
            ("EXPORT", "Xuất file", 5)
        };

        foreach (var menu in menus)
        {
            foreach (var (code, name, order) in standardActions)
            {
                actions.Add(new MenuAction
                {
                    MenuId = menu.Id,
                    Code = code,
                    Name = name,
                    Order = order
                });
            }
        }

        context.MenuActions.AddRange(actions);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} menu actions.", actions.Count);
    }

    /// <summary>
    /// Seed RoleMenuPermissions - gắn quyền cho từng Role
    /// </summary>
    private static async Task SeedRolePermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.RoleMenuPermissions.AnyAsync())
        {
            logger.LogInformation("RoleMenuPermissions already seeded.");
            return;
        }

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

        // Admin (Chủ cửa hàng): Full access trừ USER, ROLE, SETTING
        var adminExcludeMenus = new[] { "USER", "ROLE", "SETTING" };
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
        var leaderFullMenus = new[] { "ORDER", "POS", "PRODUCT", "EMPLOYEE", "DASHBOARD" };
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

        context.RoleMenuPermissions.AddRange(permissions);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} role permissions.", permissions.Count);
    }

    /// <summary>
    /// Seed Admin User mặc định
    /// </summary>
    private static async Task SeedAdminUserAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            logger.LogInformation("Admin user already exists.");
            return;
        }

        var adminRoleId = await context.Roles
            .Where(r => r.Name == "Administrator")
            .Select(r => r.Id)
            .FirstAsync();

        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "Administrator",
            Email = "admin@coffeeshop.com",
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Gắn role Administrator cho user
        context.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRoleId
        });
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded admin user: admin/Admin@123");
    }
}
