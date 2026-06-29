using System.Data;
using Dapper;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class UtilityRepository : IUtilityRepository
    {
        private readonly IDbConnection _dbConnection;

        public UtilityRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<UtilityType>> GetUtilityTypesAsync()
        {
            return await _dbConnection.QueryAsync<UtilityType>(
                "sp_Utility_GetTypes", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task SaveUtilityTypeAsync(UtilityType type)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UtilityTypeId", type.UtilityTypeId);
            parameters.Add("@Name", type.Name);
            parameters.Add("@RatePerUnit", type.RatePerUnit);
            parameters.Add("@MeasurementUnit", type.MeasurementUnit);
            parameters.Add("@IsActive", type.IsActive);

            await _dbConnection.ExecuteAsync(
                "sp_Utility_SaveType",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteUtilityTypeAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UtilityTypeId", id);

            await _dbConnection.ExecuteAsync(
                "sp_Utility_DeleteType",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<UtilityBill>> GetUtilityBillsAsync(int? month = null, int? year = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Month", month);
            parameters.Add("@Year", year);

            return await _dbConnection.QueryAsync<UtilityBill>(
                "sp_Utility_GetBills",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<decimal> GetPreviousReadingAsync(int flatId, int utilityTypeId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flatId);
            parameters.Add("@UtilityTypeId", utilityTypeId);

            return await _dbConnection.QueryFirstOrDefaultAsync<decimal>(
                "sp_Utility_GetPreviousReading",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task RecordReadingAsync(int flatId, int utilityTypeId, int month, int year, decimal currentReading, decimal? overridePreviousReading = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flatId);
            parameters.Add("@UtilityTypeId", utilityTypeId);
            parameters.Add("@BillMonth", month);
            parameters.Add("@BillYear", year);
            parameters.Add("@CurrentReading", currentReading);
            parameters.Add("@OverridePreviousReading", overridePreviousReading);

            await _dbConnection.ExecuteAsync(
                "sp_Utility_RecordReading",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateBillAsync(int billId, decimal previousReading, decimal currentReading)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);
            parameters.Add("@PreviousReading", previousReading);
            parameters.Add("@CurrentReading", currentReading);

            await _dbConnection.ExecuteAsync(
                "sp_Utility_UpdateBill",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteBillAsync(int billId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);

            await _dbConnection.ExecuteAsync(
                "sp_Utility_DeleteBill",
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
                "sp_Utility_RecordPayment",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<UtilityReceiptViewModel> GetReceiptAsync(int billId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BillId", billId);

            var bill = await _dbConnection.QueryFirstOrDefaultAsync<UtilityBill>(
                "sp_Utility_GetBillById", 
                parameters, 
                commandType: CommandType.StoredProcedure);

            var payments = await _dbConnection.QueryAsync<UtilityPayment>(
                "sp_Utility_GetPaymentsByBill", 
                parameters, 
                commandType: CommandType.StoredProcedure);

            return new UtilityReceiptViewModel
            {
                Bill = bill ?? new UtilityBill(),
                Payments = payments ?? new List<UtilityPayment>()
            };
        }

        public async Task<UtilityDashboardStats> GetDashboardStatsAsync()
        {
            return await _dbConnection.QuerySingleAsync<UtilityDashboardStats>(
                "sp_Utility_GetDashboardStats", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<UtilityBill>> GetUtilityBillsByFlatIdAsync(int flatId)
        {
            var query = @"
                SELECT 
                    B.*, 
                    UT.Name AS UtilityName, UT.MeasurementUnit, UT.RatePerUnit,
                    F.FlatNumber, B1.BlockName, 
                    O.FullName AS OwnerName, 
                    T.FullName AS TenantName
                FROM UtilityBills B
                INNER JOIN UtilityTypes UT ON B.UtilityTypeId = UT.UtilityTypeId
                INNER JOIN Flats F ON B.FlatId = F.FlatId
                INNER JOIN Blocks B1 ON F.BlockId = B1.BlockId
                LEFT JOIN Users O ON F.OwnerId = O.UserId
                LEFT JOIN Users T ON F.TenantId = T.UserId
                WHERE B.FlatId = @FlatId
                ORDER BY B.BillYear DESC, B.BillMonth DESC";

            return await _dbConnection.QueryAsync<UtilityBill>(query, new { FlatId = flatId });
        }
    }
}
