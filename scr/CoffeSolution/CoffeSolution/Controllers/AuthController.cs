using CoffeSolution.Services;
using CoffeSolution.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeSolution.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, message, user) = await _authService.LoginAsync(model.Username, model.Password);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        return RedirectToAction("Select", "StoreSelect", new { returnUrl = model.ReturnUrl });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, message) = await _authService.RegisterAsync(model);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        // Auto login after registration
        var (loginSuccess, loginMessage, user) = await _authService.LoginAsync(model.Username, model.Password);
        if (loginSuccess)
        {
             return RedirectToAction("Select", "StoreSelect");
        }

        return RedirectToAction("Login");
    }


    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _authService.GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Login");
        }

        var (success, message) = await _authService.ChangePasswordAsync(userId.Value, model.CurrentPassword, model.NewPassword);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
