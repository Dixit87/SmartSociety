using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IComplaintRepository
    {
        Task<IEnumerable<Complaint>> GetAllAsync(string status = null, int? month = null, int? year = null);
        Task<Complaint> GetByIdAsync(int complaintId);
        Task<int> CreateAsync(Complaint complaint);
        Task UpdateStatusAsync(int complaintId, string status, string adminRemarks = null);
        Task AssignAsync(int complaintId, int assignedTo);
        Task<ComplaintDashboardStats> GetDashboardStatsAsync();
        Task UpdateAsync(Complaint complaint);
        Task DeleteAsync(int complaintId);
    }
}
