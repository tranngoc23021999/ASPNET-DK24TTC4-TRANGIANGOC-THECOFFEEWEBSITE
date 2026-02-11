using CoffeSolution.Constants;
using CoffeSolution.Data;
using CoffeSolution.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeSolution.Controllers;

[Authorize]
public class POSController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthService _authService;
    private readonly IPermissionService _permissionService;
    private readonly IShiftService _shiftService;

    public POSController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService,
        IShiftService shiftService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
        _shiftService = shiftService;
    }

    public async Task<IActionResult> Index()
    {
        var storeId = HttpContext.Session.GetInt32("CurrentStoreId");
        if (storeId == null)
        {
            return RedirectToAction("Select", "StoreSelect", new { returnUrl = "/POS" });
        }

        var store = await _context.Stores.FindAsync(storeId);
        if (store == null)
        {
            return RedirectToAction("Select", "StoreSelect", new { returnUrl = "/POS" });
        }

        var userId = _authService.GetCurrentUserId();
        if (userId == null) return RedirectToAction("Login", "Auth");
        
        var user = await _context.Users.FindAsync(userId);
        ViewBag.EmployeeName = user?.FullName ?? "Unknown";

        // Check Shift
        var currentShift = await _shiftService.GetOpenShiftAsync(userId.Value);
        ViewBag.HasOpenShift = currentShift != null;
        ViewBag.ShiftId = currentShift?.Id;

        ViewBag.StoreId = storeId;
        ViewBag.StoreName = store.Name;
        ViewData["ActivePage"] = MenuCode.POS;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> StartShift([FromBody] StartShiftRequest request)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Unauthorized();

        Console.WriteLine($"[StartShift] User: {userId}, Store: {request.StoreId}, Cash: {request.StartingCash}, Note: {request.Note}");

        try
        {
            var shift = await _shiftService.StartShiftAsync(userId.Value, request.StoreId, request.StartingCash, request.Note);
            // Return back the saved data to confirm
            return Json(new { 
                success = true, 
                shiftId = shift.Id, 
                savedStartingCash = shift.StartingCash 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[StartShift Error] {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> EndShift([FromBody] EndShiftRequest request)
    {
        Console.WriteLine($"[EndShift] ShiftId: {request.ShiftId}, Cash: {request.EndingCash}");
        
        try
        {
            var shift = await _shiftService.EndShiftAsync(request.ShiftId, request.EndingCash);
             // Return back the saved data to confirm
            return Json(new { 
                success = true, 
                savedEndingCash = shift.EndingCash 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EndShift Error] {ex.Message}");
            return Json(new { success = false, message = ex.Message });
        }
    }
}

public class StartShiftRequest
{
    public int StoreId { get; set; }
    public decimal StartingCash { get; set; }
    public string? Note { get; set; }
}

public class EndShiftRequest
{
    public int ShiftId { get; set; }
    public decimal EndingCash { get; set; }
}
