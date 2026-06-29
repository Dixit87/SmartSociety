using System;

namespace SmartSociety.Models
{
    public class RfidGateLog
    {
        public int LogId { get; set; }
        public int? SlotId { get; set; }
        public string RfidTag { get; set; } = string.Empty;
        public string? VehicleNumber { get; set; }
        public string Direction { get; set; } = "Entry"; // 'Entry' or 'Exit'
        public string GateName { get; set; } = "Main Gate";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Status { get; set; } = string.Empty;

        // Joined Properties (Display)
        public string? SlotNumber { get; set; }
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? OwnerName { get; set; }

        public string FlatDisplay => !string.IsNullOrEmpty(FlatNumber) ? $"{BlockName} - {FlatNumber}" : "Visitor/External";
    }
}
