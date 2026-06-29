using System.Data;
using Dapper;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class MaintenanceRepository : IMaintenanceRepository
    {
        private readonly IDbConnection _dbConnection;

        public MaintenanceRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<MaintenanceSetting> GetSettingsAsync()
        {
            var setting = await _dbConnection.QueryFirstOrDefaultAsync<MaintenanceSetting>(
                "sp_Maintenance_GetSettings", 
                commandType: CommandType.StoredProcedure);
                
            return setting ?? new MaintenanceSetting();
        }

        public async Task UpdateSettingsAsync(MaintenanceSetting setting)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillingType", setting.BillingType);
            parameters.Add("@Rate", setting.Rate);
            parameters.Add("@PenaltyAmount", setting.PenaltyAmount);
            parameters.Add("@DueDays", setting.DueDays);
            parameters.Add("@GstEnabled", setting.GstEnabled);
            parameters.Add("@GstRate", setting.GstRate);
            parameters.Add("@GstThreshold", setting.GstThreshold);
            parameters.Add("@Gstin", setting.Gstin);
            parameters.Add("@SocietyAnnualTurnover", setting.SocietyAnnualTurnover);

            await _dbConnection.ExecuteAsync(
                "sp_Maintenance_UpdateSettings", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task GenerateBulkBillsAsync(int month, int year, decimal extraCharges, string? extraChargeRemarks)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillMonth", month);
            parameters.Add("@BillYear", year);
            parameters.Add("@ExtraCharges", extraCharges);
            parameters.Add("@ExtraChargeRemarks", extraChargeRemarks);

            await _dbConnection.ExecuteAsync(
                "sp_Maintenance_GenerateBulkBills", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<MaintenanceBill>> GetBillsAsync(int? month = null, int? year = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Month", month);
            parameters.Add("@Year", year);

            return await _dbConnection.QueryAsync<MaintenanceBill>(
                "sp_Maintenance_GetBills", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task RecordPaymentAsync(int billId, decimal paidAmount, string paymentMode, string? transactionId, string? remarks)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);
            parameters.Add("@PaidAmount", paidAmount);
            parameters.Add("@PaymentMode", paymentMode);
            parameters.Add("@TransactionId", transactionId);
            parameters.Add("@Remarks", remarks);

            await _dbConnection.ExecuteAsync(
                "sp_Maintenance_RecordPayment", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<MaintenanceDashboardStats> GetDashboardStatsAsync()
        {
            return await _dbConnection.QuerySingleAsync<MaintenanceDashboardStats>(
                "sp_Maintenance_GetDashboardStats", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<MaintenanceReceiptViewModel?> GetBillReceiptAsync(int billId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);

            using var multi = await _dbConnection.QueryMultipleAsync(
                "sp_Maintenance_GetBillReceipt",
                parameters,
                commandType: CommandType.StoredProcedure);

            var bill = await multi.ReadFirstOrDefaultAsync<MaintenanceBill>();
            if (bill == null) return null;

            var payments = (await multi.ReadAsync<BillPayment>()).ToList();

            return new MaintenanceReceiptViewModel
            {
                Bill = bill,
                Payments = payments
            };
        }

        public async Task<int> ApplyPenaltiesAsync()
        {
            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Maintenance_ApplyPenalties", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateBillAsync(int billId, decimal baseAmount, decimal extraCharges, string? extraChargeRemarks)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);
            parameters.Add("@BaseAmount", baseAmount);
            parameters.Add("@ExtraCharges", extraCharges);
            parameters.Add("@ExtraChargeRemarks", extraChargeRemarks);

            await _dbConnection.ExecuteAsync(
                "sp_Maintenance_UpdateBill",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteBillAsync(int billId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);

            await _dbConnection.ExecuteAsync(
                "sp_Maintenance_DeleteBill",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<MaintenanceBill>> GetBillsByFlatIdAsync(int flatId)
        {
            var query = @"
                SELECT 
                    B.*, 
                    F.FlatNumber, B1.BlockName, 
                    O.FullName AS OwnerName, 
                    T.FullName AS TenantName
                FROM MaintenanceBills B
                INNER JOIN Flats F ON B.FlatId = F.FlatId
                INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
                LEFT JOIN Users O ON F.OwnerId = O.UserId
                LEFT JOIN Users T ON F.TenantId = T.UserId
                WHERE B.FlatId = @FlatId
                ORDER BY B.BillYear DESC, B.BillMonth DESC";

            return await _dbConnection.QueryAsync<MaintenanceBill>(query, new { FlatId = flatId });
        }
    }
}
