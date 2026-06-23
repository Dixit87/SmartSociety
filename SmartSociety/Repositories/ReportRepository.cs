using Dapper;
using SmartSociety.Models;
using System.Data;
using System.Linq;

namespace SmartSociety.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly IDbConnection _dbConnection;

        public ReportRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<DashboardAnalyticsViewModel> GetDashboardAnalyticsAsync()
        {
            var model = new DashboardAnalyticsViewModel();

            using var multi = await _dbConnection.QueryMultipleAsync(
                "sp_Analytics_GetReportMetrics",
                commandType: CommandType.StoredProcedure);

            // 1. KPIs
            model.Metrics = await multi.ReadFirstOrDefaultAsync<ReportMetricsDto>() ?? new ReportMetricsDto();

            // 2. Complaint Categories
            model.ComplaintCategories = (await multi.ReadAsync<ComplaintCategoryDto>()).ToList();

            // 3. Bill Statuses
            model.BillStatuses = (await multi.ReadAsync<BillStatusDto>()).ToList();

            // 4. Top Defaulters
            model.TopDefaulters = (await multi.ReadAsync<DefaulterDto>()).ToList();

            return model;
        }
    }
}
