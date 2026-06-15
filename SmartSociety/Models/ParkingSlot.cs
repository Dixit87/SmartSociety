using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class ParkingSlot
    {
        public int SlotId { get; set; }

        [Required(ErrorMessage = "Slot Number is required.")]
        [StringLength(50)]
        [Display(Name = "Slot Number")]
        public string SlotNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vehicle Type is required.")]
        [StringLength(50)]
        [Display(Name = "Vehicle Type")]
        public string VehicleType { get; set; } = string.Empty;

        [Display(Name = "Assigned Flat")]
        public int? FlatId { get; set; }

        [StringLength(50)]
        [Display(Name = "Vehicle Number")]
        public string? VehicleNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Vehicle Make & Model")]
        public string? VehicleMakeModel { get; set; }

        [StringLength(50)]
        [Display(Name = "Sticker / RFID Number")]
        public string? StickerNumber { get; set; }

        [Display(Name = "Visitor Parking Slot")]
        public bool IsVisitorSlot { get; set; } = false;

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation / Extended Properties
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? OwnerName { get; set; }
        public string? TenantName { get; set; }
    }
}
