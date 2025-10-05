using IMS.Models;
using IMS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IMS.Repositories
{
    public interface IUserRepository
    {
        User? GetUserByUsername(string username);
        bool ValidateUser(string username, string password);
        public User? GetUserByUserId(int? id);
        void UpdatePassword(int userId, string newPassword);

        Task<List<SelectListItem>> GetAllRolesAsync();
        Task<User> CreateUserAsync(CreateUserViewModel model, int? createdBy);
    }
}
