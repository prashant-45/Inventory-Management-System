using IMS.Data;
using IMS.Models;
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

        public User? GetUserByUserId(int id)
        {
            return _context.Users.FirstOrDefault(u => u.Id == id && u.IsActive);
        }

        public bool ValidateUser(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == username && u.IsActive);
            if (user == null) return false;

            // ⚠️ Plaintext password (for demo). Use hashing in production.
            return user.Password == password;
        }

        public void UpdatePassword(int userId, string newPassword)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) throw new Exception("User not found");

            // 🔐 Hash password before saving
            user.Password = HashPassword(newPassword);

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
    }
}
