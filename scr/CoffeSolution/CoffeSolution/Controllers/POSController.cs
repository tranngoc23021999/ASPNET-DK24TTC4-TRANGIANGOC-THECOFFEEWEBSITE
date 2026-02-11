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

    public POSController(
        ApplicationDbContext context,
        IAuthService authService,
        IPermissionService permissionService)
    {
        _context = context;
        _authService = authService;
        _permissionService = permissionService;
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

        var user = await _context.Users.FindAsync(_authService.GetCurrentUserId());
        ViewBag.EmployeeName = user?.FullName ?? "Unknown";

        ViewBag.StoreId = storeId;
        ViewBag.StoreName = store.Name;
        ViewData["ActivePage"] = MenuCode.POS;

        return View();
    }
}
