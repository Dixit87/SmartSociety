using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class IoTRepository : IIoTRepository
    {
        private readonly IDbConnection _dbConnection;

        public IoTRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<SmartMeter>> GetSmartMetersByFlatIdAsync(int flatId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flatId);
            return await _dbConnection.QueryAsync<SmartMeter>(
                "sp_SmartMeters_GetByFlatId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<SmartMeter>> GetAllSmartMetersAsync()
        {
            return await _dbConnection.QueryAsync<SmartMeter>(
                "sp_SmartMeters_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<SmartMeter?> GetSmartMeterByIdAsync(int meterId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MeterId", meterId);
            return await _dbConnection.QueryFirstOrDefaultAsync<SmartMeter>(
                "sp_SmartMeters_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> SaveSmartMeterAsync(SmartMeter meter)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MeterId", meter.MeterId);
            parameters.Add("@FlatId", meter.FlatId);
            parameters.Add("@MeterType", meter.MeterType);
            parameters.Add("@MeterNumber", meter.MeterNumber);
            parameters.Add("@Balance", meter.Balance);
            parameters.Add("@CurrentReading", meter.CurrentReading);
            parameters.Add("@Status", meter.Status);
            parameters.Add("@IsActive", meter.IsActive);
            parameters.Add("@NewMeterId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync(
                "sp_SmartMeters_CreateOrUpdate",
                parameters,
                commandType: CommandType.StoredProcedure);

            return parameters.Get<int>("@NewMeterId");
        }

        public async Task<dynamic> ConsumeMeterBalanceAsync(int meterId, decimal unitsConsumed, decimal cost)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MeterId", meterId);
            parameters.Add("@UnitsConsumed", unitsConsumed);
            parameters.Add("@Cost", cost);

            return (await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_SmartMeters_ConsumeBalance",
                parameters,
                commandType: CommandType.StoredProcedure))!;
        }

        public async Task<dynamic> RechargeMeterAsync(int meterId, decimal amount, string paymentMethod, string transactionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MeterId", meterId);
            parameters.Add("@Amount", amount);
            parameters.Add("@PaymentMethod", paymentMethod);
            parameters.Add("@TransactionId", transactionId);

            return (await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                "sp_SmartMeters_Recharge",
                parameters,
                commandType: CommandType.StoredProcedure))!;
        }

        public async Task<IEnumerable<SmartMeterLog>> GetMeterLogsAsync(int meterId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MeterId", meterId);
            return await _dbConnection.QueryAsync<SmartMeterLog>(
                "sp_SmartMeterLogs_GetByMeterId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<SmartMeterRecharge>> GetMeterRechargesAsync(int meterId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@MeterId", meterId);
            return await _dbConnection.QueryAsync<SmartMeterRecharge>(
                "sp_SmartMeterRecharges_GetByMeterId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<RfidGateLog> InsertRfidGateLogAsync(string rfidTag, string direction, string gateName, string status)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@RfidTag", rfidTag);
            parameters.Add("@Direction", direction);
            parameters.Add("@GateName", gateName);
            parameters.Add("@Status", status);

            return (await _dbConnection.QueryFirstOrDefaultAsync<RfidGateLog>(
                "sp_RfidGateLogs_Insert",
                parameters,
                commandType: CommandType.StoredProcedure))!;
        }

        public async Task<IEnumerable<RfidGateLog>> GetAllRfidGateLogsAsync()
        {
            return await _dbConnection.QueryAsync<RfidGateLog>(
                "sp_RfidGateLogs_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<RfidGateLog>> GetRfidGateLogsByFlatIdAsync(int flatId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flatId);
            return await _dbConnection.QueryAsync<RfidGateLog>(
                "sp_RfidGateLogs_GetByFlatId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
