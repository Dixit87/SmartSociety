using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IBlockRepository
    {
        Task<IEnumerable<Block>> GetAllBlocksAsync();
        Task<int> UpsertBlockAsync(Block block);
        Task DeleteBlockAsync(int blockId);
    }
}
