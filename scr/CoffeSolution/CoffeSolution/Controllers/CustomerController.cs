using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

public class CustomerController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Customer;

    public CustomerController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
        : base(authService, permissionService)
    {
        _context = context;
    }

    [Permission(_menuId, ActionCode.View)]
    public async Task<IActionResult> Index(string? search)
    {
        await SetPermissionViewBagAsync(_menuId);

        var query = _context.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search) ||
                c.Phone.Contains(search) ||
                (c.Code != null && c.Code.Contains(search)));
        }

        var customers = await query
            .Include(c => c.Store)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        return View(customers);
    }

    private async Task<object> GetStoreOptionsAsync()
    {
        var stores = await _context.Stores
            .Where(s => s.IsActive)
            .Select(s => new { s.Id, s.Name })
            .ToListAsync();

        var options = new List<object>
        {
            new { Id = -1, Name = "Toàn hệ thống" }
        };

        // Chỉ hiện "Toàn bộ cửa hàng của tôi" nếu user có cửa hàng
        // Lưu ý: Logic này có thể cần điều chỉnh tùy requirement, 
        // ở đây tạm thời hiện luôn hoặc check qua UserStores nếu cần thiết.
        // Để đơn giản ta cứ hiện, logic xử lý ở POST sẽ check CurrentUser.
        options.Add(new { Id = -2, Name = "Toàn bộ cửa hàng của tôi" });
        
        options.AddRange(stores.Select(s => new { Id = s.Id, Name = s.Name }));
        
        return options;
    }

    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Stores = await GetStoreOptionsAsync();
        return View("CreateEdit", new Customer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(Customer model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreOptionsAsync();
            return View("CreateEdit", model);
        }

        if (await _context.Customers.AnyAsync(c => c.Code == model.Code))
        {
            ModelState.AddModelError("Code", "Mã khách hàng đã tồn tại.");
            ViewBag.Stores = await GetStoreOptionsAsync();
            return View("CreateEdit", model);
        }

        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone))
        {
            ModelState.AddModelError("Phone", "Số điện thoại đã tồn tại.");
            ViewBag.Stores = await GetStoreOptionsAsync();
            return View("CreateEdit", model);
        }

        // Xử lý Scope
        if (model.StoreId == -1) // Toàn hệ thống
        {
            model.StoreId = null;
            model.UserId = null;
        }
        else if (model.StoreId == -2) // Toàn bộ cửa hàng của tôi
        {
            model.StoreId = null;
            model.UserId = CurrentUserId;
        }
        // else: StoreId > 0, giữ nguyên

        model.CreatedAt = DateTime.Now;
        _context.Customers.Add(model);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Thêm khách hàng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        ViewBag.Stores = await GetStoreOptionsAsync();

        // Map ngược lại StoreId để hiển thị đúng trên Dropdown
        if (customer.StoreId == null)
        {
            if (customer.UserId == null) 
                customer.StoreId = -1; // Toàn hệ thống
            else 
                customer.StoreId = -2; // Cửa hàng của tôi
        }

        return View("CreateEdit", customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.Stores = await GetStoreOptionsAsync();
            return View("CreateEdit", model);
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        if (await _context.Customers.AnyAsync(c => c.Code == model.Code && c.Id != id))
        {
            ModelState.AddModelError("Code", "Mã khách hàng đã tồn tại.");
            ViewBag.Stores = await GetStoreOptionsAsync();
            return View("CreateEdit", model);
        }
        
        if (await _context.Customers.AnyAsync(c => c.Phone == model.Phone && c.Id != id))
        {
            ModelState.AddModelError("Phone", "Số điện thoại đã tồn tại.");
            ViewBag.Stores = await GetStoreOptionsAsync();
            return View("CreateEdit", model);
        }

        customer.Code = model.Code;
        customer.Name = model.Name;
        customer.Phone = model.Phone;
        customer.Email = model.Email;
        customer.Address = model.Address;
        customer.Note = model.Note;
        
        // Xử lý Scope
        if (model.StoreId == -1) // Toàn hệ thống
        {
            customer.StoreId = null;
            customer.UserId = null;
        }
        else if (model.StoreId == -2) // Toàn bộ cửa hàng của tôi
        {
            customer.StoreId = null;
            customer.UserId = CurrentUserId;
        }
        else // StoreId > 0
        {
            customer.StoreId = model.StoreId;
            customer.UserId = null; // Reset UserId nếu chọn Store cụ thể
        }

        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Cập nhật khách hàng thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        // Check dependencies if any (e.g. Orders)
        // For now, assuming no dependencies or cascade delete is handled or not required yet.
        // If there are orders linked to customer, we should check here.
        
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa khách hàng thành công!";
        return RedirectToAction(nameof(Index));
    }
}
