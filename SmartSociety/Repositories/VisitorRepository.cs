using System.Data;
using Dapper;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class VisitorRepository : IVisitorRepository
    {
        private readonly IDbConnection _dbConnection;

        public VisitorRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> EntryVisitorAsync(Visitor visitor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FullName", visitor.FullName);
            parameters.Add("@PhoneNumber", visitor.PhoneNumber);
            parameters.Add("@VisitorType", visitor.VisitorType);
            parameters.Add("@VehicleNumber", visitor.VehicleNumber);
            parameters.Add("@Purpose", visitor.Purpose);
            parameters.Add("@FlatId", visitor.FlatId);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Visitors_Entry", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task ApproveVisitorAsync(int visitorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VisitorId", visitorId);

            await _dbConnection.ExecuteAsync(
                "sp_Visitors_Approve", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task RejectVisitorAsync(int visitorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VisitorId", visitorId);

            await _dbConnection.ExecuteAsync(
                "sp_Visitors_Reject", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task CheckoutVisitorAsync(int visitorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VisitorId", visitorId);

            await _dbConnection.ExecuteAsync(
                "sp_Visitors_Checkout", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Visitor>> GetTodayVisitorsAsync()
        {
            return await _dbConnection.QueryAsync<Visitor>(
                "sp_Visitors_GetToday", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Visitor>> GetVisitorHistoryAsync(DateTime startDate, DateTime endDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@StartDate", startDate.Date);
            parameters.Add("@EndDate", endDate.Date);

            return await _dbConnection.QueryAsync<Visitor>(
                "sp_Visitors_GetHistory", 
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
