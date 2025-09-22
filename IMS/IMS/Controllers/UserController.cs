using IMS.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Authorize]
public class UserController : Controller
{
    private readonly IUserRepository _userRepo; // inject your user service/repo

    public UserController(IUserRepository userService)
    {
        _userRepo = userService;
    }

    public IActionResult Profile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var user = _userRepo.GetUserByUserId(userId); // get user details from DB

        return View(user);
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
