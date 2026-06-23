using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface INoticeRepository
    {
        Task<IEnumerable<Notice>> GetAllNoticesAsync(string? status = null, string? category = null);
        Task<Notice?> GetNoticeByIdAsync(int noticeId);
        Task<int> UpsertNoticeAsync(Notice notice);
        Task DeleteNoticeAsync(int noticeId);
        Task TogglePinStatusAsync(int noticeId, bool isPinned);
    }
}
