using CoffeSolution.Attributes;
using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

public class SupplierController : BaseController
{
    private readonly ApplicationDbContext _context;
    private const string _menuId = MenuCode.Supplier;

    public SupplierController(
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

        var query = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Phone != null && s.Phone.Contains(search)) ||
                (s.ContactPerson != null && s.ContactPerson.Contains(search)));
        }

        var suppliers = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        ViewBag.Search = search;
        return View(suppliers);
    }

    [Permission(_menuId, ActionCode.Create)]
    public IActionResult Create()
    {
        return View(new SupplierViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Create)]
    public async Task<IActionResult> Create(SupplierViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var supplier = new Supplier
        {
            Name = model.Name,
            Address = model.Address,
            Phone = model.Phone,
            Email = model.Email,
            ContactPerson = model.ContactPerson,
            IsActive = model.IsActive,
            CreatedAt = DateTime.Now
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Tạo nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        var model = new SupplierViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Address = supplier.Address,
            Phone = supplier.Phone,
            Email = supplier.Email,
            ContactPerson = supplier.ContactPerson,
            IsActive = supplier.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Edit)]
    public async Task<IActionResult> Edit(int id, SupplierViewModel model)
    {
        if (id != model.Id) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        supplier.Name = model.Name;
        supplier.Address = model.Address;
        supplier.Phone = model.Phone;
        supplier.Email = model.Email;
        supplier.ContactPerson = model.ContactPerson;
        supplier.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Cập nhật nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission(_menuId, ActionCode.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        TempData[TempDataKey.Success] = "Xóa nhà cung cấp thành công!";
        return RedirectToAction(nameof(Index));
    }
}
