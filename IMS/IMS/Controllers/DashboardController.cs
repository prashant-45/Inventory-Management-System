using IMS.Repositories.Interfaces;
using IMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace IMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDeliveryChallanRepository _challanRepo;

        public DashboardController(IDeliveryChallanRepository challanRepo)
        {
            _challanRepo = challanRepo;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            bool isAdmin = roles.Contains("Admin");

            var viewModel = new DashboardViewModel
            {
                IsAdmin = isAdmin
            };

            // Get last 5 days
            var last5Days = Enumerable.Range(0, 5)
                .Select(i => DateTime.Today.AddDays(-i))
                .Reverse()
                .ToList();

            viewModel.Last5Days = last5Days.Select(d => d.ToString("dd MMM")).ToList();

            // Prepare challans per day
            //List<int> counts = new();

            var challans = isAdmin
                ? await _challanRepo.GetChallansForDashboardAsync(0, "admin") // 0 or any value, ignored
                : await _challanRepo.GetChallansForDashboardAsync(userId, "");


            var counts = last5Days
                .Select(day => challans.Count(c => c.Date.Date == day))
                .ToList();

            viewModel.ChallansPerDay = counts;

            // Total count
            viewModel.TotalChallans = challans.Count();

            return View(viewModel);
        }
    }

}
