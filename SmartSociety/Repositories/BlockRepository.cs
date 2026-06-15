using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class BlockRepository : IBlockRepository
    {
        private readonly IDbConnection _dbConnection;

        public BlockRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Block>> GetAllBlocksAsync()
        {
            return await _dbConnection.QueryAsync<Block>(
                "sp_Blocks_GetAll", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertBlockAsync(Block block)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BlockId", block.BlockId);
            parameters.Add("@BlockName", block.BlockName);
            parameters.Add("@TotalFloors", block.TotalFloors);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Blocks_Upsert", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteBlockAsync(int blockId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@BlockId", blockId);

            await _dbConnection.ExecuteAsync(
                "sp_Blocks_Delete", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
    }
}
