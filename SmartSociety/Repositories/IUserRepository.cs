using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int userId);
        Task<int> UpsertUserAsync(User user);
        Task DeleteUserAsync(int userId);
    }
}
