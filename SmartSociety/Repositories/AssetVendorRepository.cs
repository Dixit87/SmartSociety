using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class AssetVendorRepository : IAssetVendorRepository
    {
        private readonly IDbConnection _dbConnection;

        public AssetVendorRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        // Vendor Operations
        public async Task<IEnumerable<Vendor>> GetAllVendorsAsync(string? status = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Status", status);

            return await _dbConnection.QueryAsync<Vendor>(
                "sp_Vendors_GetAll",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Vendor?> GetVendorByIdAsync(int vendorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VendorId", vendorId);

            return await _dbConnection.QueryFirstOrDefaultAsync<Vendor>(
                "sp_Vendors_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertVendorAsync(Vendor vendor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VendorId", vendor.VendorId);
            parameters.Add("@VendorName", vendor.VendorName);
            parameters.Add("@ServiceCategory", vendor.ServiceCategory);
            parameters.Add("@ContactPerson", vendor.ContactPerson);
            parameters.Add("@PhoneNumber", vendor.PhoneNumber);
            parameters.Add("@Email", vendor.Email);
            parameters.Add("@ContractStartDate", vendor.ContractStartDate);
            parameters.Add("@ContractEndDate", vendor.ContractEndDate);
            parameters.Add("@ContractCost", vendor.ContractCost);
            parameters.Add("@Status", vendor.Status);
            parameters.Add("@ContractDocumentPath", vendor.ContractDocumentPath);
            parameters.Add("@Rating", vendor.Rating);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Vendors_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteVendorAsync(int vendorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VendorId", vendorId);

            await _dbConnection.ExecuteAsync(
                "sp_Vendors_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        // Asset Operations
        public async Task<IEnumerable<Asset>> GetAllAssetsAsync(string? status = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Status", status);

            return await _dbConnection.QueryAsync<Asset>(
                "sp_Assets_GetAll",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Asset?> GetAssetByIdAsync(int assetId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AssetId", assetId);

            return await _dbConnection.QueryFirstOrDefaultAsync<Asset>(
                "sp_Assets_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertAssetAsync(Asset asset)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AssetId", asset.AssetId);
            parameters.Add("@AssetName", asset.AssetName);
            parameters.Add("@AssetType", asset.AssetType);
            parameters.Add("@Location", asset.Location);
            parameters.Add("@PurchaseDate", asset.PurchaseDate);
            parameters.Add("@PurchaseCost", asset.PurchaseCost);
            parameters.Add("@VendorId", asset.VendorId == 0 ? (int?)null : asset.VendorId);
            parameters.Add("@AmcExpiryDate", asset.AmcExpiryDate);
            parameters.Add("@Status", asset.Status);
            parameters.Add("@InvoiceDocumentPath", asset.InvoiceDocumentPath);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Assets_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteAssetAsync(int assetId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AssetId", assetId);

            await _dbConnection.ExecuteAsync(
                "sp_Assets_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        // Service Log Operations
        public async Task<IEnumerable<AssetServiceLog>> GetServiceLogsByAssetIdAsync(int assetId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@AssetId", assetId);

            return await _dbConnection.QueryAsync<AssetServiceLog>(
                "sp_AssetServiceLogs_GetByAssetId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertServiceLogAsync(AssetServiceLog log)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@LogId", log.LogId);
            parameters.Add("@AssetId", log.AssetId);
            parameters.Add("@VendorId", log.VendorId == 0 ? (int?)null : log.VendorId);
            parameters.Add("@ServiceDate", log.ServiceDate);
            parameters.Add("@Description", log.Description);
            parameters.Add("@Cost", log.Cost);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_AssetServiceLogs_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteServiceLogAsync(int logId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@LogId", logId);

            await _dbConnection.ExecuteAsync(
                "sp_AssetServiceLogs_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
