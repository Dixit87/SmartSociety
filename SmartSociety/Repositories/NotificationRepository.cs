using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IDbConnection _dbConnection;

        public NotificationRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            return await _dbConnection.QueryAsync<Notification>(
                "sp_Notifications_GetByUserId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertAsync(Notification notification)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", notification.UserId);
            parameters.Add("@Title", notification.Title);
            parameters.Add("@Message", notification.Message);
            parameters.Add("@Category", notification.Category);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Notifications_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@NotificationId", notificationId);

            await _dbConnection.ExecuteAsync(
                "sp_Notifications_MarkAsRead",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            await _dbConnection.ExecuteAsync(
                "sp_Notifications_MarkAllAsRead",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteAsync(int notificationId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@NotificationId", notificationId);

            await _dbConnection.ExecuteAsync(
                "sp_Notifications_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
