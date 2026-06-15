using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Visitor
    {
        public int VisitorId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone Number is required.")]
        [StringLength(20)]
        [Phone]
        [Display(Name = "Mobile Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Visitor Type is required.")]
        [StringLength(50)]
        [Display(Name = "Visitor Type")]
        public string VisitorType { get; set; } = string.Empty; // Guest, Delivery, Maid/Helper, Vendor

        [StringLength(50)]
        [Display(Name = "Vehicle Number")]
        public string? VehicleNumber { get; set; }

        [StringLength(255)]
        [Display(Name = "Purpose of Visit")]
        public string? Purpose { get; set; }

        [Required(ErrorMessage = "Please select a Flat.")]
        [Display(Name = "Visiting Flat")]
        public int FlatId { get; set; }

        public DateTime InTime { get; set; } = DateTime.Now;
        public DateTime? OutTime { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        // Navigation / Extended Properties
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }

        public bool IsOverstaying => Status == "Inside" && (DateTime.Now - InTime).TotalHours > 4;
    }
}
