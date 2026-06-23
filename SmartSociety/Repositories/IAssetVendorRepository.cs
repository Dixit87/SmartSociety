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
    }
}
