using System.Collections.Generic;

namespace SmartSociety.Models
{
    public class ResidentDashboardViewModel
    {
        public Flat ResidentFlat { get; set; } = new Flat();
        public decimal OutstandingMaintenanceDues { get; set; }
        public decimal OutstandingUtilityDues { get; set; }
        public int ActiveComplaintsCount { get; set; }
        public int ActiveNoticesCount { get; set; }
        
        public List<MaintenanceBill> RecentBills { get; set; } = new();
        public List<UtilityBill> RecentUtilityBills { get; set; } = new();
        public List<Complaint> RecentComplaints { get; set; } = new();
        public List<Notice> RecentNotices { get; set; } = new();
        public List<Poll> RecentPolls { get; set; } = new();
    }
}
