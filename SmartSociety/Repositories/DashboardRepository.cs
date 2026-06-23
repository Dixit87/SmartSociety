using Dapper;
using SmartSociety.Models;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnection _dbConnection;

        public DashboardRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<AdminDashboardViewModel> GetAdminSummaryAsync()
        {
            var model = new AdminDashboardViewModel();

            using var multi = await _dbConnection.QueryMultipleAsync(
                "sp_Dashboard_GetAdminSummary",
                commandType: CommandType.StoredProcedure);

            model.KPIs = await multi.ReadFirstOrDefaultAsync<DashboardKPIs>() ?? new DashboardKPIs();
            model.RecentVisitors = (await multi.ReadAsync<RecentVisitorDto>()).ToList();
            model.RecentComplaints = (await multi.ReadAsync<RecentComplaintDto>()).ToList();
            model.RecentPayments = (await multi.ReadAsync<RecentPaymentDto>()).ToList();
            model.RevenueChart = (await multi.ReadAsync<MonthlyRevenueDto>()).ToList();
            model.ComplaintStats = (await multi.ReadAsync<ComplaintStatDto>()).ToList();

            return model;
        }
    }
}
