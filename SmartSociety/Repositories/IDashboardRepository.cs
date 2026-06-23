using SmartSociety.Models;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface IDashboardRepository
    {
        Task<AdminDashboardViewModel> GetAdminSummaryAsync();
    }
}
