using System;
using System.Collections.Generic;

namespace SmartSociety.Models
{
    public class DashboardKPIs
    {
        public int TotalFlats { get; set; }
        public int TotalResidents { get; set; }
        public int ActiveStaff { get; set; }
        public int OpenComplaints { get; set; }
        public int ActiveNotices { get; set; }
        public decimal TotalRevenueThisMonth { get; set; }
    }

    public class RecentVisitorDto
    {
        public string VisitorName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string HostFlat { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class RecentComplaintDto
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RaisedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class RecentPaymentDto
    {
        public int BillMonth { get; set; }
        public int BillYear { get; set; }
        public string FlatNumber { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class ComplaintStatDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public DashboardKPIs KPIs { get; set; } = new DashboardKPIs();
        public List<RecentVisitorDto> RecentVisitors { get; set; } = new List<RecentVisitorDto>();
        public List<RecentComplaintDto> RecentComplaints { get; set; } = new List<RecentComplaintDto>();
        public List<RecentPaymentDto> RecentPayments { get; set; } = new List<RecentPaymentDto>();
        
        public List<MonthlyRevenueDto> RevenueChart { get; set; } = new List<MonthlyRevenueDto>();
        public List<ComplaintStatDto> ComplaintStats { get; set; } = new List<ComplaintStatDto>();
    }
}
