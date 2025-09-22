using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";

            // ✅ Get logged-in user details
            var userName = User.Identity?.Name;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // ✅ Get roles from claims
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            // ✅ Pass data to view
            ViewBag.UserName = userName;
            ViewBag.UserId = userId;
            ViewBag.Roles = roles;

            return View();
        }

    }
}
