using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Controllers;

[Authorize]
public class ShiftController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;

    public ShiftController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
    }

    public async Task<IActionResult> Index(int? storeId, DateTime? fromDate, DateTime? toDate)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");
        
        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, MenuCode.Shift, ActionCode.View);
        if (!hasPermission) return Forbid();

        ViewData["ActivePage"] = MenuCode.Shift;
        ViewBag.Stores = await _context.Stores.ToListAsync();

        var query = _context.Shifts
            .Include(s => s.Staff)
            .Include(s => s.Store)
            .AsQueryable();

        if (storeId.HasValue)
        {
            query = query.Where(s => s.StoreId == storeId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.StartTime.Date >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            query = query.Where(s => s.StartTime.Date <= toDate.Value.Date);
        }

        var canEdit = await _permissionService.HasPermissionAsync(userId.Value, MenuCode.Shift, ActionCode.Edit);
        ViewBag.CanEdit = canEdit;

        var shifts = await query.OrderByDescending(s => s.StartTime).ToListAsync();

        return View(shifts);
    }
    
    public async Task<IActionResult> Details(int id)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");
        
        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, MenuCode.Shift, ActionCode.View);
        if (!hasPermission) return Forbid();

        var shift = await _context.Shifts
            .Include(s => s.Staff)
            .Include(s => s.Store)
            .Include(s => s.Orders)
                .ThenInclude(o => o.OrderDetails)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shift == null) return NotFound();

        var canEdit = await _permissionService.HasPermissionAsync(userId.Value, MenuCode.Shift, ActionCode.Edit);
        ViewBag.CanEdit = canEdit;

        ViewData["ActivePage"] = MenuCode.Shift;
        return View(shift);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");

        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, MenuCode.Shift, ActionCode.Edit);
        if (!hasPermission) return Forbid();

        var shift = await _context.Shifts
            .Include(s => s.Staff)
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shift == null) return NotFound();
        
        ViewData["ActivePage"] = MenuCode.Shift;
        return View(shift);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,StartingCash,EndingCash")] Shift shift)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");

        var hasPermission = await _permissionService.HasPermissionAsync(userId.Value, MenuCode.Shift, ActionCode.Edit);
        if (!hasPermission) return Forbid();

        if (id != shift.Id) return NotFound();

        var existingShift = await _context.Shifts.FindAsync(id);
        if (existingShift == null) return NotFound();

        existingShift.StartingCash = shift.StartingCash;
        existingShift.EndingCash = shift.EndingCash;
        
        // Recalculate difference/update note if needed? No, just save amount.
        
        try
        {
            _context.Update(existingShift);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ShiftExists(shift.Id)) return NotFound();
            else throw;
        }
        return RedirectToAction(nameof(Index));
    }

    private bool ShiftExists(int id)
    {
        return _context.Shifts.Any(e => e.Id == id);
    }
}
