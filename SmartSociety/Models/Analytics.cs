using System;
using System.Collections.Generic;

namespace SmartSociety.Models
{
    public class ReportMetricsDto
    {
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }
        public int OpenComplaints { get; set; }
        public int TotalBookings { get; set; }
    }

    public class ComplaintCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class BillStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class DefaulterDto
    {
        public string ResidentName { get; set; } = string.Empty;
        public string FlatNumber { get; set; } = string.Empty;
        public decimal DueAmount { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class DashboardAnalyticsViewModel
    {
        public ReportMetricsDto Metrics { get; set; } = new ReportMetricsDto();
        public List<ComplaintCategoryDto> ComplaintCategories { get; set; } = new List<ComplaintCategoryDto>();
        public List<BillStatusDto> BillStatuses { get; set; } = new List<BillStatusDto>();
        public List<DefaulterDto> TopDefaulters { get; set; } = new List<DefaulterDto>();
    }
}
