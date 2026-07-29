using System.Security.Claims;
using Tezgah.Models;
using Tezgah.Repositories;
using Tezgah.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tezgah.Controllers;

public class AccountController : Controller
{
    private readonly UserRepository _userRepo;
    private readonly GroupRepository _groupRepo;

    public AccountController(UserRepository userRepo, GroupRepository groupRepo)
    {
        _userRepo = userRepo;
        _groupRepo = groupRepo;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userRepo.GetByEmailAsync(model.Email);

        if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Email veya şifre hatalı.");
            return View(model);
        }

        await SignInUserAsync(user, model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    [Authorize]
    public IActionResult Profile()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View("Profile", model);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userRepo.GetByIdAsync(userId);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError("CurrentPassword", "Mevcut şifre hatalı.");
            return View("Profile", model);
        }

        var newHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        await _userRepo.UpdatePasswordAsync(userId, newHash);

        TempData["Success"] = "Şifreniz başarıyla güncellendi.";
        return RedirectToAction("Profile");
    }

    private async Task SignInUserAsync(AppUser user, bool rememberMe)
    {
        var group = user.GroupId.HasValue
            ? await _groupRepo.GetByIdAsync(user.GroupId.Value)
            : null;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("FullName", user.FullName),
            new("IsSuperAdmin", user.IsSuperAdmin ? "true" : "false"),
            new("IsReadOnly",   user.IsReadOnly   ? "true" : "false"),
        };

        if (user.GroupId.HasValue)
            claims.Add(new("GroupId", user.GroupId.Value.ToString()));

        if (group != null)
        {
            claims.Add(new("GroupName",        group.Name));
            claims.Add(new("CanCreateProjects", group.CanCreateProjects ? "true" : "false"));
            claims.Add(new("CanManageTasks",    group.CanManageTasks    ? "true" : "false"));
            claims.Add(new("CanViewReports",    group.CanViewReports    ? "true" : "false"));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);
    }
}
