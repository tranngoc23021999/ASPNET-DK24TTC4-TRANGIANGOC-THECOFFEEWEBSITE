using System.Security.Claims;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password);
    Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model);
    Task LogoutAsync();
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<User?> GetCurrentUserAsync();
    int? GetCurrentUserId();
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<(bool Success, string Message, User? User)> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            return (false, "Tên đăng nhập không tồn tại hoặc tài khoản đã bị khóa.", null);
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return (false, "Mật khẩu không chính xác.", null);
        }

        // Tạo Claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName),
        };

        // Thêm roles vào claims
        foreach (var userRole in user.UserRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Name));
        }

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await _httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return (true, "Đăng nhập thành công.", user);
    }

    public async Task<(bool Success, string Message)> RegisterAsync(RegisterViewModel model)
    {
        // 1. Check existing username
        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            return (false, "Tên đăng nhập đã tồn tại.");
        }

        // 2. Create User
        var user = new User
        {
            Username = model.Username,
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        // 3. Assign Default Role (Admin)
        // Note: Assuming "Admin" role exists. If not, we might need seeding or handling.
        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole != null)
        {
            user.UserRoles.Add(new UserRole { Role = adminRole });
        }
        else
        {
             // Fallback or Log error? For now, let's proceed but maybe without role or create one? 
             // Ideally roles are seeded. Let's assume seeded.
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, "Đăng ký thành công.");
    }

    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "Không tìm thấy người dùng.");
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return (false, "Mật khẩu hiện tại không chính xác.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _context.SaveChangesAsync();

        return (true, "Đổi mật khẩu thành công.");
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return null;

        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.UserStores)
                .ThenInclude(us => us.Store)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public int? GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }
        return userId;
    }
}
