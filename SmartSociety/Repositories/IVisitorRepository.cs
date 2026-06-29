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
        Task<IEnumerable<Visitor>> GetByFlatIdAsync(int flatId);
        Task<int> PreRegisterVisitorAsync(Visitor visitor);
        Task<Visitor?> GetVisitorByIdAsync(int visitorId);
        
        // Invite system additions
        Task<Visitor?> GetVisitorByInviteCodeAsync(string code);
        Task<int> PreRegisterVisitorWithInviteAsync(Visitor visitor);

        // Delivery management additions
        Task<IEnumerable<Delivery>> GetDeliveriesByFlatIdAsync(int flatId);
        Task<IEnumerable<Delivery>> GetTodayDeliveriesAsync();
        Task<int> InsertDeliveryAsync(Delivery delivery);
        Task CollectDeliveryAsync(int deliveryId);
        Task<Delivery?> GetDeliveryByIdAsync(int deliveryId);

        // Child Safety additions
        Task<int> InsertChildExitRequestAsync(ChildExitRequest request);
        Task<IEnumerable<ChildExitRequest>> GetTodayChildExitRequestsAsync();
        Task<IEnumerable<ChildExitRequest>> GetChildExitRequestsByFlatIdAsync(int flatId);
        Task<ChildExitRequest?> GetChildExitRequestByIdAsync(int requestId);
        Task UpdateChildExitRequestStatusAsync(int requestId, string status);
    }
}
