using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnection _dbConnection;

        public UserRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _dbConnection.QueryAsync<User>(
                "sp_Users_GetAll", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            return await _dbConnection.QueryFirstOrDefaultAsync<User>(
                "sp_Users_GetById", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertUserAsync(User user)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", user.UserId);
            parameters.Add("@FullName", user.FullName);
            parameters.Add("@Email", user.Email);
            parameters.Add("@PhoneNumber", user.PhoneNumber);
            parameters.Add("@PasswordHash", user.PasswordHash);
            parameters.Add("@Role", user.Role);
            parameters.Add("@FlatNumber", user.FlatNumber);
            parameters.Add("@ProfilePicture", user.ProfilePicture);
            parameters.Add("@IsActive", user.IsActive);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Users_Upsert", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteUserAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            await _dbConnection.ExecuteAsync(
                "sp_Users_Delete", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
    }
}
