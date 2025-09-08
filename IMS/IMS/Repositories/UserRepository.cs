using IMS.Data;
using IMS.Models;

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

        public bool ValidateUser(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == username && u.IsActive);
            if (user == null) return false;

            // ⚠️ Plaintext password (for demo). Use hashing in production.
            return user.Password == password;
        }
    }
}
