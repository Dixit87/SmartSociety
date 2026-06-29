using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IAssetVendorRepository
    {
        // Vendor Operations
        Task<IEnumerable<Vendor>> GetAllVendorsAsync(string? status = null);
        Task<Vendor?> GetVendorByIdAsync(int vendorId);
        Task<int> UpsertVendorAsync(Vendor vendor);
        Task DeleteVendorAsync(int vendorId);

        // Asset Operations
        Task<IEnumerable<Asset>> GetAllAssetsAsync(string? status = null);
        Task<Asset?> GetAssetByIdAsync(int assetId);
        Task<int> UpsertAssetAsync(Asset asset);
        Task DeleteAssetAsync(int assetId);

        // Service Log Operations
        Task<IEnumerable<AssetServiceLog>> GetServiceLogsByAssetIdAsync(int assetId);
        Task<int> UpsertServiceLogAsync(AssetServiceLog log);
        Task DeleteServiceLogAsync(int logId);

        // Maintenance Schedule Operations
        Task<IEnumerable<MaintenanceSchedule>> GetMaintenanceSchedulesAsync();
        Task<MaintenanceSchedule?> GetMaintenanceScheduleByIdAsync(int scheduleId);
        Task<int> UpsertMaintenanceScheduleAsync(MaintenanceSchedule schedule);
        Task DeleteMaintenanceScheduleAsync(int scheduleId);
        Task<int> ProcessDueMaintenanceSchedulesAsync();

        // Inventory Operations
        Task<IEnumerable<InventoryItem>> GetAllInventoryItemsAsync();
        Task<InventoryItem?> GetInventoryItemByIdAsync(int itemId);
        Task<int> UpsertInventoryItemAsync(InventoryItem item);
        Task DeleteInventoryItemAsync(int itemId);
        Task DeductInventoryStockAsync(int itemId, int quantityUsed);

        // Complaint Spare Parts Operations
        Task<IEnumerable<ComplaintSparePart>> GetSparePartsByComplaintIdAsync(int complaintId);
        Task SaveComplaintSparePartAsync(ComplaintSparePart part);
    }
}
