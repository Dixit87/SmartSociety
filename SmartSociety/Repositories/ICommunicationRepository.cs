using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface ICommunicationRepository
    {
        Task<IEnumerable<MessageLog>> GetAllMessageLogsAsync();
        Task<int> LogMessageAsync(MessageLog log);
    }
}
