using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IFlatRepository
    {
        Task<IEnumerable<Flat>> GetAllFlatsAsync();
        Task<Flat?> GetFlatByIdAsync(int flatId);
        Task<int> UpsertFlatAsync(Flat flat);
        Task DeleteFlatAsync(int flatId);
    }
}
