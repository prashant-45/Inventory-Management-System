using IMS.Data;
using IMS.Models;
using IMS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IMS.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public User? GetUserByUsername(string username)
        {
            return _context.Users.FirstOrDefault(u => u.UserName == username && u.IsActive);
        }

        public User? GetUserByUserId(int? id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id && u.IsActive);
        }

        //public bool ValidateUser(string username, string password)
        //{
        //    var user = _context.Users.FirstOrDefault(u => u.UserName == username && u.IsActive);
        //    if (user == null) return false;

        //    // ⚠️ Plaintext password (for demo). Use hashing in production.
        //    return user.Password == password;
        //}

        public void UpdatePassword(int userId, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) throw new Exception("User not found");

            // 🔐 Hash password using PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, newPassword);

            _context.Users.Update(user);
            _context.SaveChanges();
        }

        private string HashPassword(string password)
        {
            // Example using SHA256 (better to use ASP.NET Identity’s PasswordHasher)
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public async Task<List<SelectListItem>> GetAllRolesAsync()
        {
            return await _context.Roles
                .Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name
                })
                .ToListAsync();
        }

        // 🆕 Create User + Assign Role
        public async Task<User> CreateUserAsync(CreateUserViewModel model, int? createdBy)
        {
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                MobileNumber = model.MobileNumber,
                IsActive = true,
                CreatedBy = createdBy
                // CreatedAt = DateTime.Now (optional)
            };

            // Hash the password using PasswordHasher
            var passwordHasher = new PasswordHasher<User>();
            user.Password = passwordHasher.HashPassword(user, model.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Assign role
            var userRole = new UserRole
            {
                Fk_UserId = user.Id,
                Fk_RoleId = model.SelectedRoleId,
                IsActive = true,
                CreatedBy = createdBy
                // CreatedAt = DateTime.Now (optional)
            };

            await _context.UserRoles.AddAsync(userRole);
            await _context.SaveChangesAsync();

            return user;
        }

        public bool ValidateUser(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == username && u.IsActive);
            if (user == null) return false;

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.Password, password);

            return result != PasswordVerificationResult.Failed;
        }

    }
}
