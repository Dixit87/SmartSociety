using System;

namespace SmartSociety.Models
{
    public class ChildExitRequest
    {
        public int RequestId { get; set; }
        public int FlatId { get; set; }
        public int FamilyMemberId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? GuardRemarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ActionedAt { get; set; }

        // Extended properties
        public string? ChildName { get; set; }
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? ResidentName { get; set; }
    }
}
