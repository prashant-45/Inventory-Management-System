using IMS.Data;
using IMS.Models;
using IMS.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IMS.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _context;

        public AuthService(IUserRepository userRepository, ApplicationDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public User? Login(string username, string password)
        {
            if (_userRepository.ValidateUser(username, password))
            {
                return _userRepository.GetUserByUsername(username);
            }
            return null;
        }

        // ✅ Fetch roles for a specific user
        public List<string> GetUserRoles(int userId)
        {
            return _context.UserRoles
                           .Where(ur => ur.Fk_UserId == userId)
                           .Select(ur => ur.Role.Name)
                           .ToList();
        }

        public List<string> GetUserPermissions(int userId)
        {
            return _context.UserPermissions
                .Where(up => up.fk_UserId == userId && up.IsActive)
                .Select(up => up.Permission.Name) // Permission table join
                .ToList();
        }
    }
}
