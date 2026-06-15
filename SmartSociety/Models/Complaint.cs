using System;
using System.Collections.Generic;

namespace SmartSociety.Models
{
    public class Complaint
    {
        public int ComplaintId { get; set; }
        public int FlatId { get; set; }
        public string BlockName { get; set; }
        public string FlatNumber { get; set; }
        public int RaisedBy { get; set; }
        public string ResidentName { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public int? AssignedTo { get; set; }
        public string TechnicianName { get; set; }
        public string AdminRemarks { get; set; }
        public string PhotoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public string GetPriorityBadgeClass()
        {
            return Priority switch
            {
                "Emergency" => "bg-danger",
                "High" => "bg-warning text-dark",
                "Medium" => "bg-info text-dark",
                "Low" => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        public string GetStatusBadgeClass()
        {
            return Status switch
            {
                "Open" => "bg-primary-subtle text-primary border border-primary-subtle",
                "InProgress" => "bg-warning-subtle text-warning-emphasis border border-warning-subtle",
                "Resolved" => "bg-success-subtle text-success border border-success-subtle",
                "Closed" => "bg-secondary-subtle text-secondary border border-secondary-subtle",
                _ => "bg-light text-dark"
            };
        }
        
        public string GetStatusIcon()
        {
            return Status switch
            {
                "Open" => "fa-solid fa-envelope-open-text",
                "InProgress" => "fa-solid fa-person-digging",
                "Resolved" => "fa-solid fa-check-double",
                "Closed" => "fa-solid fa-lock",
                _ => "fa-solid fa-circle"
            };
        }
    }

    public class ComplaintDashboardStats
    {
        public int TotalComplaints { get; set; }
        public int OpenComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int HighPriorityPending { get; set; }
    }
}
