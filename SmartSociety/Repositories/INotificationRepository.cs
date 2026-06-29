using SmartSociety.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
        Task<int> InsertAsync(Notification notification);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task DeleteAsync(int notificationId);
    }
}
