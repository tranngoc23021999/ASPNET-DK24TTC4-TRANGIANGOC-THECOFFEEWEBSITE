using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

/// <summary>
/// Controller quản lý Users với RBAC hierarchy:
/// - Administrator: Quản lý tất cả users
/// - Admin: Quản lý Leader, Staff trong stores của mình
/// </summary>
public class UserController : BaseController
{
    private readonly ApplicationDbContext _context;

    public UserController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    #region Index - Danh sách users

    [Permission(MenuCode.User, ActionCode.View)]
    public async Task<IActionResult> Index(string? search, int? roleId, int? storeId, int page = 1)
    {
        await SetPermissionViewBagAsync(MenuCode.User);
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);
        
        // Query users
        var query = _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserStores).ThenInclude(us => us.Store)
            .AsQueryable();

        // Filter theo quyền:
        // - Administrator: thấy tất cả
        // - Admin: chỉ thấy users trong stores của mình (và users do mình quản lý)
        if (!isAdministrator)
        {
            var myStoreIds = await _context.UserStores
                .Where(us => us.UserId == currentUser.Id)
                .Select(us => us.StoreId)
                .ToListAsync();

            query = query.Where(u => 
                u.Id == currentUser.Id || // Bản thân
                u.AdminId == currentUser.Id || // Users do mình tạo
                u.UserStores.Any(us => myStoreIds.Contains(us.StoreId))); // Users trong stores của mình
        }

        // Filter by search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(u => 
                u.Username.ToLower().Contains(search) ||
                u.FullName.ToLower().Contains(search) ||
                (u.Email != null && u.Email.ToLower().Contains(search)) ||
                (u.Phone != null && u.Phone.Contains(search)));
        }

        // Filter by role
        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));
        }

        // Filter by store
        if (storeId.HasValue)
        {
            query = query.Where(u => u.UserStores.Any(us => us.StoreId == storeId));
        }

        // Pagination
        const int pageSize = 20;
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListViewModel
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Phone = u.Phone,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                RoleName = u.UserRoles.Select(ur => ur.Role.Name).FirstOrDefault(),
                StoreNames = u.UserStores.Select(us => us.Store.Name).ToList()
            })
            .ToListAsync();

        // Build filter
        var filter = await BuildStoreFilterAsync(currentUser, isAdministrator);

        var viewModel = new UserIndexViewModel
        {
            Users = users,
            Filter = filter,
            SearchTerm = search,
            FilterRoleId = roleId,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount
        };

        ViewBag.Roles = await GetRoleSelectListAsync(currentUser, isAdministrator);
        ViewBag.Stores = await GetStoreSelectListAsync(currentUser, isAdministrator);

        return View(viewModel);
    }

    #endregion

    #region Details

    [Permission(MenuCode.User, ActionCode.View)]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserStores).ThenInclude(us => us.Store)
            .Include(u => u.Admin)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            TempData[TempDataKey.Error] = Messages.UserNotFound;
            return RedirectToAction(nameof(Index));
        }

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);
        
        // Check quyền xem
        if (!await CanViewUserAsync(currentUser, isAdministrator, user))
        {
            TempData[TempDataKey.Error] = Messages.AccessDenied;
            return RedirectToAction(nameof(Index));
        }

        await SetPermissionViewBagAsync(MenuCode.User);
        return View(user);
    }

    #endregion

    #region Create

    [Permission(MenuCode.User, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);

        var viewModel = new CreateUserViewModel
        {
            AvailableRoles = await GetAssignableRolesAsync(currentUser, isAdministrator),
            AvailableStores = await GetAssignableStoresAsync(currentUser, isAdministrator)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(MenuCode.User, ActionCode.Create)]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);

        if (ModelState.IsValid)
        {
            // Validate username unique
            if (await _context.Users.AnyAsync(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
            }
            // Validate role
            else if (!await CanAssignRoleAsync(currentUser, isAdministrator, model.RoleId))
            {
                ModelState.AddModelError("RoleId", "Bạn không có quyền gán vai trò này");
            }
            // Validate stores - chỉ bắt buộc cho role không phải Admin
            var selectedRole = await _context.Roles.FindAsync(model.RoleId);
            var isAdminRole = selectedRole?.Name == "Admin";
            
            if (!isAdminRole && !model.StoreIds.Any())
            {
                ModelState.AddModelError("StoreIds", "Vui lòng chọn ít nhất 1 cửa hàng cho vai trò này");
            }
            else if (model.StoreIds.Any() && !await CanAssignStoresAsync(currentUser, isAdministrator, model.StoreIds))
            {
                ModelState.AddModelError("StoreIds", "Bạn không có quyền gán cửa hàng này");
            }
            else
            {
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    IsActive = true,
                    AdminId = isAdministrator ? null : currentUser.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Add role
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = model.RoleId,
                    AssignedAt = DateTime.Now
                });

                // Add stores
                bool isFirst = true;
                foreach (var storeId in model.StoreIds)
                {
                    _context.UserStores.Add(new UserStore
                    {
                        UserId = user.Id,
                        StoreId = storeId,
                        IsDefault = isFirst,
                        AssignedAt = DateTime.Now
                    });
                    isFirst = false;
                }

                await _context.SaveChangesAsync();

                TempData[TempDataKey.Success] = Messages.CreateSuccess;
                return RedirectToAction(nameof(Index));
            }
        }

        model.AvailableRoles = await GetAssignableRolesAsync(currentUser, isAdministrator);
        model.AvailableStores = await GetAssignableStoresAsync(currentUser, isAdministrator);
        return View(model);
    }

    #endregion

    #region Edit

    [Permission(MenuCode.User, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserStores)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            TempData[TempDataKey.Error] = Messages.UserNotFound;
            return RedirectToAction(nameof(Index));
        }

        // Check quyền edit
        if (!await CanManageUserAsync(currentUser, isAdministrator, user))
        {
            TempData[TempDataKey.Error] = Messages.AccessDenied;
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new EditUserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            RoleId = user.UserRoles.FirstOrDefault()?.RoleId ?? 0,
            StoreIds = user.UserStores.Select(us => us.StoreId).ToList(),
            IsActive = user.IsActive,
            AvailableRoles = await GetAssignableRolesAsync(currentUser, isAdministrator),
            AvailableStores = await GetAssignableStoresAsync(currentUser, isAdministrator)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(MenuCode.User, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, EditUserViewModel model)
    {
        if (id != model.Id) return NotFound();

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.UserStores)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            TempData[TempDataKey.Error] = Messages.UserNotFound;
            return RedirectToAction(nameof(Index));
        }

        if (!await CanManageUserAsync(currentUser, isAdministrator, user))
        {
            TempData[TempDataKey.Error] = Messages.AccessDenied;
            return RedirectToAction(nameof(Index));
        }

        if (ModelState.IsValid)
        {
            if (!await CanAssignRoleAsync(currentUser, isAdministrator, model.RoleId))
            {
                ModelState.AddModelError("RoleId", "Bạn không có quyền gán vai trò này");
            }
            // Validate stores - chỉ bắt buộc cho role không phải Admin
            var selectedRole = await _context.Roles.FindAsync(model.RoleId);
            var isAdminRole = selectedRole?.Name == "Admin";
            
            if (!isAdminRole && !model.StoreIds.Any())
            {
                ModelState.AddModelError("StoreIds", "Vui lòng chọn ít nhất 1 cửa hàng cho vai trò này");
            }
            else if (model.StoreIds.Any() && !await CanAssignStoresAsync(currentUser, isAdministrator, model.StoreIds))
            {
                ModelState.AddModelError("StoreIds", "Bạn không có quyền gán cửa hàng này");
            }
            else
            {
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.IsActive = model.IsActive;

                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                }

                // Update role
                _context.UserRoles.RemoveRange(user.UserRoles);
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = model.RoleId,
                    AssignedAt = DateTime.Now
                });

                // Update stores
                _context.UserStores.RemoveRange(user.UserStores);
                bool isFirst = true;
                foreach (var storeId in model.StoreIds)
                {
                    _context.UserStores.Add(new UserStore
                    {
                        UserId = user.Id,
                        StoreId = storeId,
                        IsDefault = isFirst,
                        AssignedAt = DateTime.Now
                    });
                    isFirst = false;
                }

                await _context.SaveChangesAsync();

                TempData[TempDataKey.Success] = Messages.UpdateSuccess;
                return RedirectToAction(nameof(Index));
            }
        }

        model.AvailableRoles = await GetAssignableRolesAsync(currentUser, isAdministrator);
        model.AvailableStores = await GetAssignableStoresAsync(currentUser, isAdministrator);
        return View(model);
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(MenuCode.User, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return RedirectToAction("Login", "Auth");

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);

        // Không xóa chính mình
        if (id == currentUser.Id)
        {
            TempData[TempDataKey.Error] = "Không thể xóa tài khoản của chính mình!";
            return RedirectToAction(nameof(Index));
        }

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.UserStores)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            TempData[TempDataKey.Error] = Messages.UserNotFound;
            return RedirectToAction(nameof(Index));
        }

        if (!await CanManageUserAsync(currentUser, isAdministrator, user))
        {
            TempData[TempDataKey.Error] = Messages.AccessDenied;
            return RedirectToAction(nameof(Index));
        }

        // Không xóa Administrator
        if (user.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
        {
            TempData[TempDataKey.Error] = "Không thể xóa tài khoản Administrator!";
            return RedirectToAction(nameof(Index));
        }

        // Check if user is an Owner
        var ownsStores = await _context.Stores.AnyAsync(s => s.OwnerId == user.Id);
        if (ownsStores)
        {
            TempData[TempDataKey.Error] = "Không thể xóa tài khoản đang là Chủ sở hữu cửa hàng!";
            return RedirectToAction(nameof(Index));
        }

        // Check history
        var hasOrders = await _context.Orders.AnyAsync(o => o.StaffId == user.Id);
        if (hasOrders)
        {
            TempData[TempDataKey.Error] = "Không thể xóa tài khoản đã phát sinh đơn hàng!";
            return RedirectToAction(nameof(Index));
        }

        var hasReceipts = await _context.WarehouseReceipts.AnyAsync(wr => wr.CreatedById == user.Id);
        if (hasReceipts)
        {
            TempData[TempDataKey.Error] = "Không thể xóa tài khoản đã tạo phiếu nhập kho!";
            return RedirectToAction(nameof(Index));
        }

        _context.UserRoles.RemoveRange(user.UserRoles);
        _context.UserStores.RemoveRange(user.UserStores);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = Messages.DeleteSuccess;
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Toggle Status

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(MenuCode.User, ActionCode.Edit)]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null) return Json(new { success = false });

        var isAdministrator = await PermissionService.IsAdministratorAsync(currentUser.Id);

        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return Json(new { success = false, message = "User không tồn tại" });

        if (!await CanManageUserAsync(currentUser, isAdministrator, user))
        {
            return Json(new { success = false, message = "Bạn không có quyền thực hiện" });
        }

        // Không disable Administrator
        if (user.UserRoles.Any(ur => ur.Role.Name == "Administrator"))
        {
            return Json(new { success = false, message = "Không thể vô hiệu hóa Administrator" });
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();

        return Json(new { success = true, isActive = user.IsActive });
    }

    #endregion

    #region Helper Methods

    private async Task<List<Role>> GetAssignableRolesAsync(User currentUser, bool isAdministrator)
    {
        if (isAdministrator)
        {
            return await _context.Roles.OrderBy(r => r.Id).ToListAsync();
        }
        
        // Admin chỉ gán được Leader, Staff
        return await _context.Roles
            .Where(r => r.Name == "Leader" || r.Name == "Staff")
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    private async Task<List<Store>> GetAssignableStoresAsync(User currentUser, bool isAdministrator)
    {
        if (isAdministrator)
        {
            return await _context.Stores.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync();
        }

        // Admin thấy: stores được gán (UserStores) + stores sở hữu (OwnerId)
        var assignedStoreIds = await _context.UserStores
            .Where(us => us.UserId == currentUser.Id)
            .Select(us => us.StoreId)
            .ToListAsync();

        return await _context.Stores
            .Where(s => s.IsActive && (
                assignedStoreIds.Contains(s.Id) ||  // Stores được gán
                s.OwnerId == currentUser.Id         // Stores sở hữu
            ))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    private async Task<bool> CanAssignRoleAsync(User currentUser, bool isAdministrator, int roleId)
    {
        var roles = await GetAssignableRolesAsync(currentUser, isAdministrator);
        return roles.Any(r => r.Id == roleId);
    }

    private async Task<bool> CanAssignStoresAsync(User currentUser, bool isAdministrator, List<int> storeIds)
    {
        // Empty list is allowed for Admin role (validated elsewhere)
        if (!storeIds.Any()) return true;
        var stores = await GetAssignableStoresAsync(currentUser, isAdministrator);
        var ids = stores.Select(s => s.Id).ToHashSet();
        return storeIds.All(id => ids.Contains(id));
    }

    private async Task<bool> CanViewUserAsync(User currentUser, bool isAdministrator, User targetUser)
    {
        if (isAdministrator) return true;
        if (targetUser.Id == currentUser.Id) return true;
        
        // User do mình tạo
        if (targetUser.AdminId == currentUser.Id) return true;

        // Stores được gán + stores sở hữu
        var myAssignedStoreIds = await _context.UserStores
            .Where(us => us.UserId == currentUser.Id)
            .Select(us => us.StoreId)
            .ToListAsync();
        
        var myOwnedStoreIds = await _context.Stores
            .Where(s => s.OwnerId == currentUser.Id)
            .Select(s => s.Id)
            .ToListAsync();
        
        var allMyStoreIds = myAssignedStoreIds.Union(myOwnedStoreIds).ToList();

        return targetUser.UserStores.Any(us => allMyStoreIds.Contains(us.StoreId));
    }

    private async Task<bool> CanManageUserAsync(User currentUser, bool isAdministrator, User targetUser)
    {
        if (isAdministrator) return true;
        if (targetUser.Id == currentUser.Id) return false; // Không tự edit/delete mình qua đây

        var targetRoles = targetUser.UserRoles.Select(ur => ur.Role?.Name ?? "").ToList();
        if (targetRoles.Contains("Administrator") || targetRoles.Contains("Admin"))
            return false;
        
        // User do mình tạo -> có quyền manage
        if (targetUser.AdminId == currentUser.Id) return true;

        // Stores được gán + stores sở hữu
        var myAssignedStoreIds = await _context.UserStores
            .Where(us => us.UserId == currentUser.Id)
            .Select(us => us.StoreId)
            .ToListAsync();
        
        var myOwnedStoreIds = await _context.Stores
            .Where(s => s.OwnerId == currentUser.Id)
            .Select(s => s.Id)
            .ToListAsync();
        
        var allMyStoreIds = myAssignedStoreIds.Union(myOwnedStoreIds).ToList();

        return targetUser.UserStores.Any(us => allMyStoreIds.Contains(us.StoreId));
    }

    private async Task<List<SelectListItem>> GetRoleSelectListAsync(User currentUser, bool isAdministrator)
    {
        var roles = await GetAssignableRolesAsync(currentUser, isAdministrator);
        return roles.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name }).ToList();
    }

    private async Task<List<SelectListItem>> GetStoreSelectListAsync(User currentUser, bool isAdministrator)
    {
        var stores = await GetAssignableStoresAsync(currentUser, isAdministrator);
        return stores.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToList();
    }

    private async Task<StoreFilterViewModel> BuildStoreFilterAsync(User currentUser, bool isAdministrator)
    {
        var filter = new StoreFilterViewModel
        {
            IsAdministrator = isAdministrator,
            IsAdmin = !isAdministrator
        };

        if (isAdministrator)
        {
            filter.AdminFilters = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserStores)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                .Select(u => new AdminFilterItem
                {
                    AdminId = u.Id,
                    AdminName = u.FullName,
                    StoreCount = u.UserStores.Count
                })
                .ToListAsync();

            filter.StoreFilters = await _context.Stores
                .Where(s => s.IsActive)
                .Select(s => new StoreOptionItem
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Address = s.Address
                })
                .ToListAsync();
        }
        else
        {
            filter.StoreFilters = await _context.UserStores
                .Include(us => us.Store)
                .Where(us => us.UserId == currentUser.Id && us.Store.IsActive)
                .Select(us => new StoreOptionItem
                {
                    Id = us.Store.Id,
                    Name = us.Store.Name,
                    Code = us.Store.Code,
                    Address = us.Store.Address
                })
                .ToListAsync();
        }

        return filter;
    }

    #endregion
}
