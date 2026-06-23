using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class CommunicationRepository : ICommunicationRepository
    {
        private readonly IDbConnection _dbConnection;

        public CommunicationRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<MessageLog>> GetAllMessageLogsAsync()
        {
            return await _dbConnection.QueryAsync<MessageLog>(
                "sp_MessageLogs_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> LogMessageAsync(MessageLog log)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MessageType", log.MessageType);
            parameters.Add("@Audience", log.Audience);
            parameters.Add("@Subject", log.Subject);
            parameters.Add("@Body", log.Body);
            parameters.Add("@Status", log.Status);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_MessageLogs_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
