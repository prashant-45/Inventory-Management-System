using IMS.Models;
using IMS.Repositories;
using IMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using System.Security.Claims;

[Authorize]
public class UserController : Controller
{
    private readonly IUserRepository _userRepo; // inject your user service/repo

    public UserController(IUserRepository userService)
    {
        _userRepo = userService;
    }

    public IActionResult Profile(int? Id)
    {
        var userId = Id > 0 ? Id : int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = _userRepo.GetUserByUserId(userId); // get user details from DB

        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateUserViewModel
        {
            Roles = await _userRepo.GetAllRolesAsync(),
            Branches = new List<SelectListItem>
            {
                new() { Value = "Rajasthan", Text = "Rajasthan" },
                new() { Value = "Gujarat", Text = "Gujarat" },
                new() { Value = "Maharashtra", Text = "Maharashtra" },
                new() { Value = "Delhi", Text = "Delhi" }
            }
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = await _userRepo.GetAllRolesAsync();

            // ✅ repopulate static branches
            model.Branches = new List<SelectListItem>
            {
                new() { Value = "Rajasthan", Text = "Rajasthan" },
                new() { Value = "Gujarat", Text = "Gujarat" },
                new() { Value = "Maharashtra", Text = "Maharashtra" },
                new() { Value = "Delhi", Text = "Delhi" }
            };
            return View(model);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Pass the model to repository, PasswordHasher will handle hashing
        await _userRepo.CreateUserAsync(model, createdBy: userId);

        TempData["Success"] = "User created successfully!";
        return RedirectToAction("ManagePermissions", "Admin");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = _userRepo.GetUserByUsername(model.UserName);
        if (user == null)
        {
            ModelState.AddModelError("", "User not found.");
            return View(model);
        }

        _userRepo.UpdatePassword(user.Id, model.NewPassword); // hashing handled inside repo

        TempData["Success"] = "Password updated successfully! You can now login.";
        return RedirectToAction("Login", "Account");
    }

    [HttpPost]
    public IActionResult ChangePassword(int userId, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
        {
            return BadRequest("Passwords do not match.");
        }

        _userRepo.UpdatePassword(userId, newPassword); // implement hashing inside service
        return Ok("Password updated successfully!");
    }
}
