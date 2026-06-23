using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IAuthRepository
    {
        Task<User?> ValidateUserAsync(string email, string password);
    }
}
