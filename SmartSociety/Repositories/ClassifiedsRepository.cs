using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class ClassifiedsRepository : IClassifiedsRepository
    {
        private readonly IDbConnection _dbConnection;

        public ClassifiedsRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<ClassifiedAd>> GetAllActiveAdsAsync()
        {
            return await _dbConnection.QueryAsync<ClassifiedAd>(
                "sp_Classifieds_GetAllActive",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<ClassifiedAd?> GetAdByIdAsync(int adId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ClassifiedId", adId);

            return await _dbConnection.QueryFirstOrDefaultAsync<ClassifiedAd>(
                "sp_Classifieds_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<ClassifiedAd>> GetAdsByUserIdAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            return await _dbConnection.QueryAsync<ClassifiedAd>(
                "sp_Classifieds_GetByUserId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> SaveAdAsync(ClassifiedAd ad)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ClassifiedId", ad.ClassifiedId);
            parameters.Add("@UserId", ad.UserId);
            parameters.Add("@Title", ad.Title);
            parameters.Add("@Description", ad.Description);
            parameters.Add("@Price", ad.Price);
            parameters.Add("@AdCategory", ad.AdCategory);
            parameters.Add("@AdType", ad.AdType);
            parameters.Add("@ImagePath", ad.ImagePath);
            parameters.Add("@IsActive", ad.IsActive);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Classifieds_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteAdAsync(int adId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ClassifiedId", adId);

            await _dbConnection.ExecuteAsync(
                "sp_Classifieds_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
