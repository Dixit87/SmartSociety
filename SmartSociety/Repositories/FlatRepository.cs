using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class FlatRepository : IFlatRepository
    {
        private readonly IDbConnection _dbConnection;

        public FlatRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Flat>> GetAllFlatsAsync()
        {
            return await _dbConnection.QueryAsync<Flat>(
                "sp_Flats_GetAll", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Flat?> GetFlatByIdAsync(int flatId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flatId);

            return await _dbConnection.QueryFirstOrDefaultAsync<Flat>(
                "sp_Flats_GetById", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertFlatAsync(Flat flat)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flat.FlatId);
            parameters.Add("@BlockId", flat.BlockId);
            parameters.Add("@FlatNumber", flat.FlatNumber);
            parameters.Add("@FloorNumber", flat.FloorNumber);
            parameters.Add("@FlatType", flat.FlatType);
            parameters.Add("@AreaSqFt", flat.AreaSqFt);
            parameters.Add("@OwnerId", flat.OwnerId);
            parameters.Add("@TenantId", flat.TenantId);
            parameters.Add("@IsActive", flat.IsActive);
            parameters.Add("@IntercomNumber", flat.IntercomNumber);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Flats_Upsert", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteFlatAsync(int flatId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", flatId);

            await _dbConnection.ExecuteAsync(
                "sp_Flats_Delete", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Flat?> GetFlatByUserIdAsync(int userId)
        {
            var query = @"
                SELECT TOP 1 f.*, b.BlockName 
                FROM Flats f 
                INNER JOIN Blocks b ON f.BlockId = b.BlockId 
                WHERE f.OwnerId = @UserId OR f.TenantId = @UserId";
            
            return await _dbConnection.QueryFirstOrDefaultAsync<Flat>(query, new { UserId = userId });
        }
    }
}
