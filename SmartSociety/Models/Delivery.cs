using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Delivery
    {
        public int DeliveryId { get; set; }

        [Required]
        public int FlatId { get; set; }

        [Required(ErrorMessage = "Company/Courier is required.")]
        [StringLength(100)]
        public string Company { get; set; } = string.Empty; // Amazon, Zomato, Swiggy, etc.

        [Required(ErrorMessage = "Agent Name is required.")]
        [StringLength(100)]
        [Display(Name = "Agent Name")]
        public string DeliveryAgentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Agent Phone is required.")]
        [StringLength(20)]
        [Display(Name = "Agent Contact")]
        public string DeliveryAgentPhone { get; set; } = string.Empty;

        [StringLength(50)]
        public string Status { get; set; } = "LoggedAtGate"; // LoggedAtGate, Collected

        [StringLength(255)]
        public string? ReceiptPhoto { get; set; }

        public DateTime LoggedAt { get; set; } = DateTime.Now;
        public DateTime? CollectedAt { get; set; }

        // Extended properties
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? ResidentName { get; set; }
    }
}
