using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IMaintenanceRepository
    {
        Task<MaintenanceSetting> GetSettingsAsync();
        Task UpdateSettingsAsync(MaintenanceSetting setting);
        Task GenerateBulkBillsAsync(int month, int year, decimal extraCharges, string? extraChargeRemarks);
        Task<IEnumerable<MaintenanceBill>> GetBillsAsync(int? month = null, int? year = null);
        Task<IEnumerable<MaintenanceBill>> GetBillsByFlatIdAsync(int flatId);
        Task RecordPaymentAsync(int billId, decimal paidAmount, string paymentMode, string? transactionId, string? remarks);
        Task<MaintenanceDashboardStats> GetDashboardStatsAsync();
        Task<MaintenanceReceiptViewModel?> GetBillReceiptAsync(int billId);
        Task<int> ApplyPenaltiesAsync();
        Task UpdateBillAsync(int billId, decimal baseAmount, decimal extraCharges, string? extraChargeRemarks);
        Task DeleteBillAsync(int billId);
    }
}
