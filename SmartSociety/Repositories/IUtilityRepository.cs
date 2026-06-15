using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IUtilityRepository
    {
        Task<IEnumerable<UtilityType>> GetUtilityTypesAsync();
        Task SaveUtilityTypeAsync(UtilityType type);
        Task DeleteUtilityTypeAsync(int id);
        
        Task<IEnumerable<UtilityBill>> GetUtilityBillsAsync(int? month = null, int? year = null);
        Task<decimal> GetPreviousReadingAsync(int flatId, int utilityTypeId);
        Task RecordReadingAsync(int flatId, int utilityTypeId, int month, int year, decimal currentReading, decimal? overridePreviousReading = null);
        Task UpdateBillAsync(int billId, decimal previousReading, decimal currentReading);
        Task DeleteBillAsync(int billId);
        Task RecordPaymentAsync(int billId, decimal paidAmount, string paymentMode, string? transactionId, string? remarks);
        Task<UtilityReceiptViewModel> GetReceiptAsync(int billId);
        
        Task<UtilityDashboardStats> GetDashboardStatsAsync();
    }
}
