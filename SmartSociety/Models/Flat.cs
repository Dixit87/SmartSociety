using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Flat
    {
        public int FlatId { get; set; }
        
        [Required(ErrorMessage = "Please select a block")]
        [Display(Name = "Block")]
        public int BlockId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Flat Number")]
        public string FlatNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Floor Number")]
        [Range(0, 200, ErrorMessage = "Invalid Floor Number")]
        public int FloorNumber { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Flat Type")]
        public string FlatType { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Area (Sq. Ft.)")]
        [Range(100, 10000, ErrorMessage = "Invalid Area")]
        public decimal AreaSqFt { get; set; }

        [Display(Name = "Owner")]
        public int? OwnerId { get; set; }

        [Display(Name = "Tenant")]
        public int? TenantId { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        [Display(Name = "Intercom Number")]
        public string? IntercomNumber { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // View-only properties (populated by JOINs in SP)
        public string? BlockName { get; set; }
        public string? OwnerName { get; set; }
        public string? TenantName { get; set; }
    }
}
