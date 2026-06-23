using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IReportRepository
    {
        Task<DashboardAnalyticsViewModel> GetDashboardAnalyticsAsync();
    }
}
