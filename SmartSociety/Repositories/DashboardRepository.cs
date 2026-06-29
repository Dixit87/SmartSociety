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

        public async Task<ResidentDashboardViewModel> GetResidentSummaryAsync(int userId, int flatId)
        {
            var model = new ResidentDashboardViewModel();

            // Fetch Flat details
            var flatQuery = @"
                SELECT f.*, b.BlockName, O.FullName AS OwnerName, T.FullName AS TenantName 
                FROM Flats f 
                INNER JOIN Blocks b ON f.BlockId = b.BlockId 
                LEFT JOIN Users O ON f.OwnerId = O.UserId
                LEFT JOIN Users T ON f.TenantId = T.UserId
                WHERE f.FlatId = @FlatId";
            model.ResidentFlat = await _dbConnection.QueryFirstOrDefaultAsync<Flat>(flatQuery, new { FlatId = flatId }) ?? new Flat();

            // Fetch Dues
            var maintenanceDuesQuery = "SELECT ISNULL(SUM(TotalAmount - AmountPaid), 0) FROM MaintenanceBills WHERE FlatId = @FlatId AND Status != 'Paid'";
            model.OutstandingMaintenanceDues = await _dbConnection.ExecuteScalarAsync<decimal>(maintenanceDuesQuery, new { FlatId = flatId });

            var utilityDuesQuery = "SELECT ISNULL(SUM(TotalAmount - AmountPaid), 0) FROM UtilityBills WHERE FlatId = @FlatId AND Status != 'Paid'";
            model.OutstandingUtilityDues = await _dbConnection.ExecuteScalarAsync<decimal>(utilityDuesQuery, new { FlatId = flatId });

            // Active Complaints count (filed by resident)
            var activeComplaintsQuery = "SELECT COUNT(*) FROM Complaints WHERE FlatId = @FlatId AND Status != 'Resolved'";
            model.ActiveComplaintsCount = await _dbConnection.ExecuteScalarAsync<int>(activeComplaintsQuery, new { FlatId = flatId });

            // Active Notices count
            var activeNoticesQuery = "SELECT COUNT(*) FROM Notices WHERE IsActive = 1 AND (ValidTill IS NULL OR ValidTill >= GETDATE())";
            model.ActiveNoticesCount = await _dbConnection.ExecuteScalarAsync<int>(activeNoticesQuery);

            // Recent bills (top 5)
            var recentBillsQuery = @"
                SELECT TOP 5 B.*, F.FlatNumber, B1.BlockName 
                FROM MaintenanceBills B
                INNER JOIN Flats F ON B.FlatId = F.FlatId
                INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
                WHERE B.FlatId = @FlatId
                ORDER BY B.BillYear DESC, B.BillMonth DESC";
            model.RecentBills = (await _dbConnection.QueryAsync<MaintenanceBill>(recentBillsQuery, new { FlatId = flatId })).ToList();

            // Recent utility bills (top 5)
            var recentUtilityBillsQuery = @"
                SELECT TOP 5 B.*, UT.Name AS UtilityName, UT.MeasurementUnit 
                FROM UtilityBills B
                INNER JOIN UtilityTypes UT ON B.UtilityTypeId = UT.UtilityTypeId
                WHERE B.FlatId = @FlatId
                ORDER BY B.BillYear DESC, B.BillMonth DESC";
            model.RecentUtilityBills = (await _dbConnection.QueryAsync<UtilityBill>(recentUtilityBillsQuery, new { FlatId = flatId })).ToList();

            // Recent complaints (top 5)
            var recentComplaintsQuery = @"
                SELECT TOP 5 * FROM Complaints 
                WHERE FlatId = @FlatId
                ORDER BY CreatedAt DESC";
            model.RecentComplaints = (await _dbConnection.QueryAsync<Complaint>(recentComplaintsQuery, new { FlatId = flatId })).ToList();

            // Recent notices (top 5)
            var recentNoticesQuery = @"
                SELECT TOP 5 * FROM Notices 
                WHERE IsActive = 1 AND (ValidTill IS NULL OR ValidTill >= GETDATE())
                ORDER BY IsPinned DESC, CreatedAt DESC";
            model.RecentNotices = (await _dbConnection.QueryAsync<Notice>(recentNoticesQuery)).ToList();

            // Recent active polls (top 5)
            var recentPollsQuery = @"
                SELECT TOP 5 * FROM Polls 
                WHERE StartDate <= GETDATE() AND EndDate >= GETDATE()
                ORDER BY CreatedAt DESC";
            model.RecentPolls = (await _dbConnection.QueryAsync<Poll>(recentPollsQuery)).ToList();

            return model;
        }
    }
}
