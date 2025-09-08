using IMS.Models;

namespace IMS.Repositories
{
    public interface IUserRepository
    {
        User? GetUserByUsername(string username);
        bool ValidateUser(string username, string password);
    }
}
