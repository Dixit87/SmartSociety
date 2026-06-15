using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IVisitorRepository
    {
        Task<int> EntryVisitorAsync(Visitor visitor);
        Task ApproveVisitorAsync(int visitorId);
        Task RejectVisitorAsync(int visitorId);
        Task CheckoutVisitorAsync(int visitorId);
        Task<IEnumerable<Visitor>> GetTodayVisitorsAsync();
        Task<IEnumerable<Visitor>> GetVisitorHistoryAsync(DateTime startDate, DateTime endDate);
    }
}
