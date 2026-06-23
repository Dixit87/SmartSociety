using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbConnection _dbConnection;

        public AuthRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var query = @"
                SELECT * FROM Users 
                WHERE Email = @Email 
                AND IsActive = 1";

            var user = await _dbConnection.QueryFirstOrDefaultAsync<User>(query, new { Email = email });

            if (user != null && !string.IsNullOrEmpty(user.PasswordHash))
            {
                // Verify the plain text password against the stored BCrypt hash
                if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return user;
                }
            }

            return null;
        }
    }
}
