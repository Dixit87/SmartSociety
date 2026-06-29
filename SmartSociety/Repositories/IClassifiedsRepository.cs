using SmartSociety.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface IClassifiedsRepository
    {
        Task<IEnumerable<ClassifiedAd>> GetAllActiveAdsAsync();
        Task<ClassifiedAd?> GetAdByIdAsync(int adId);
        Task<IEnumerable<ClassifiedAd>> GetAdsByUserIdAsync(int userId);
        Task<int> SaveAdAsync(ClassifiedAd ad);
        Task DeleteAdAsync(int adId);
    }
}
