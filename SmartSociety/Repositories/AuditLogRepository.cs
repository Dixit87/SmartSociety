using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly IDbConnection _dbConnection;

        public AuditLogRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<AuditLog>> GetAllLogsAsync()
        {
            return await _dbConnection.QueryAsync<AuditLog>(
                "sp_AuditLogs_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task InsertLogAsync(AuditLog log)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Username", log.Username);
            parameters.Add("@ActionType", log.ActionType);
            parameters.Add("@ModuleName", log.ModuleName);
            parameters.Add("@Description", log.Description);
            parameters.Add("@IPAddress", log.IPAddress);

            await _dbConnection.ExecuteAsync(
                "sp_AuditLogs_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
