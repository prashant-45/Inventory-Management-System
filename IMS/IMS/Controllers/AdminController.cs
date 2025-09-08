using IMS.Data;
using IMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Show list of users with permission management option
        public async Task<IActionResult> ManagePermissions()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // Load permissions for a specific user
        public async Task<IActionResult> EditPermissions(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var allPermissions = await _context.Permissions
                                    .Include(p => p.PermissionGroup)
                                    .ToListAsync();

            var userPermissions = await _context.UserPermissions
                                    .Where(up => up.fk_UserId == userId)
                                    .Select(up => up.fk_PermissionId)
                                    .ToListAsync();

            var model = new UserPermissionViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Permissions = allPermissions.Select(p => new PermissionCheckbox
                {
                    PermissionId = p.Id,
                    PermissionName = $"{p.PermissionGroup.Name} - {p.Name}",
                    IsAssigned = userPermissions.Contains(p.Id)
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditPermissions(UserPermissionViewModel model)
        {
            var existing = _context.UserPermissions.Where(up => up.fk_UserId == model.UserId);
            _context.UserPermissions.RemoveRange(existing);

            foreach (var perm in model.Permissions.Where(p => p.IsAssigned))
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    fk_UserId = model.UserId,
                    fk_PermissionId = perm.PermissionId
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagePermissions");
        }
    }
}
