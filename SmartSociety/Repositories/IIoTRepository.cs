using SmartSociety.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface IIoTRepository
    {
        // Smart Meters
        Task<IEnumerable<SmartMeter>> GetSmartMetersByFlatIdAsync(int flatId);
        Task<IEnumerable<SmartMeter>> GetAllSmartMetersAsync();
        Task<SmartMeter?> GetSmartMeterByIdAsync(int meterId);
        Task<int> SaveSmartMeterAsync(SmartMeter meter);
        Task<dynamic> ConsumeMeterBalanceAsync(int meterId, decimal unitsConsumed, decimal cost);
        Task<dynamic> RechargeMeterAsync(int meterId, decimal amount, string paymentMethod, string transactionId);
        Task<IEnumerable<SmartMeterLog>> GetMeterLogsAsync(int meterId);
        Task<IEnumerable<SmartMeterRecharge>> GetMeterRechargesAsync(int meterId);

        // RFID Gate
        Task<RfidGateLog> InsertRfidGateLogAsync(string rfidTag, string direction, string gateName, string status);
        Task<IEnumerable<RfidGateLog>> GetAllRfidGateLogsAsync();
        Task<IEnumerable<RfidGateLog>> GetRfidGateLogsByFlatIdAsync(int flatId);
    }
}
