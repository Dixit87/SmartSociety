using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class SmartMeter
    {
        public int MeterId { get; set; }

        [Required(ErrorMessage = "Please select a Flat")]
        public int FlatId { get; set; }

        [Required(ErrorMessage = "Please select Meter Type")]
        [StringLength(50)]
        public string MeterType { get; set; } = "Electricity"; // 'Electricity' or 'Water'

        [Required(ErrorMessage = "Meter Number is required")]
        [StringLength(100)]
        public string MeterNumber { get; set; } = string.Empty;

        [Range(0, 100000, ErrorMessage = "Balance must be positive")]
        public decimal Balance { get; set; } = 0.00m;

        [Range(0, 9999999, ErrorMessage = "Reading must be positive")]
        public decimal CurrentReading { get; set; } = 0.00m;

        public DateTime LastSyncTime { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Active"; // 'Active' or 'Suspended'

        public bool IsActive { get; set; } = true;

        // Joined Properties (Display)
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? OwnerName { get; set; }
        public decimal RatePerUnit { get; set; }
        public string? MeasurementUnit { get; set; }

        public string FlatDisplay => $"{BlockName ?? ""} - {FlatNumber ?? ""}";
    }
}
