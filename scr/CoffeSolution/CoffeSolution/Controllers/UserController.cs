using CoffeSolution.Attributes;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

public class UserController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string MenuCode = "USER";

    public UserController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission("USER", "VIEW")]
    public async Task<IActionResult> Index(string? search, int? roleId)
    {
        await SetPermissionViewBagAsync(MenuCode);

        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserStores)
                .ThenInclude(us => us.Store)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u =>
                u.Username.Contains(search) ||
                u.FullName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.RoleId = roleId;
        ViewBag.Roles = await GetRoleSelectListAsync();

        return View(users);
    }

    [Permission("USER", "VIEW")]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserStores)
                .ThenInclude(us => us.Store)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        await SetPermissionViewBagAsync(MenuCode);
        return View(user);
    }

    [Permission("USER", "CREATE")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Roles = await GetRoleSelectListAsync();
        ViewBag.Stores = await GetStoreSelectListAsync();
        return View(new UserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("USER", "CREATE")]
    public async Task<IActionResult> Create(UserViewModel model)
    {
        if (string.IsNullOrEmpty(model.Password))
        {
            ModelState.AddModelError("Password", "Vui lòng nhập mật khẩu");
        }

        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await GetRoleSelectListAsync();
            ViewBag.Stores = await GetStoreSelectListAsync();
            return View(model);
        }

        var user = new User
        {
            Username = model.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            IsActive = model.IsActive,
            AdminId = model.AdminId ?? CurrentUserId,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Gắn Roles
        foreach (var roleId in model.RoleIds)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedAt = DateTime.Now
            });
        }

        // Gắn Stores
        foreach (var storeId in model.StoreIds)
        {
            _context.UserStores.Add(new UserStore
            {
                UserId = user.Id,
                StoreId = storeId,
                AssignedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Tạo người dùng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Permission("USER", "EDIT")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserStores)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        var model = new UserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            IsActive = user.IsActive,
            AdminId = user.AdminId,
            RoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList(),
            StoreIds = user.UserStores.Select(us => us.StoreId).ToList()
        };

        ViewBag.Roles = await GetRoleSelectListAsync();
        ViewBag.Stores = await GetStoreSelectListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("USER", "EDIT")]
    public async Task<IActionResult> Edit(int id, UserViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await GetRoleSelectListAsync();
            ViewBag.Stores = await GetStoreSelectListAsync();
            return View(model);
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserStores)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        user.Username = model.Username;
        user.FullName = model.FullName;
        user.Email = model.Email;
        user.Phone = model.Phone;
        user.IsActive = model.IsActive;
        user.AdminId = model.AdminId;

        if (!string.IsNullOrEmpty(model.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        }

        // Cập nhật Roles
        _context.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in model.RoleIds)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                AssignedAt = DateTime.Now
            });
        }

        // Cập nhật Stores
        _context.UserStores.RemoveRange(user.UserStores);
        foreach (var storeId in model.StoreIds)
        {
            _context.UserStores.Add(new UserStore
            {
                UserId = user.Id,
                StoreId = storeId,
                AssignedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("USER", "DELETE")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserStores)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return NotFound();

        // Không cho xóa chính mình
        if (user.Id == CurrentUserId)
        {
            TempData["ErrorMessage"] = "Không thể xóa tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        _context.UserRoles.RemoveRange(user.UserRoles);
        _context.UserStores.RemoveRange(user.UserStores);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Xóa người dùng thành công!";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetRoleSelectListAsync()
    {
        return await _context.Roles
            .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetStoreSelectListAsync()
    {
        return await _context.Stores
            .Where(s => s.IsActive)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
            .ToListAsync();
    }
}
