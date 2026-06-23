using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Notice
    {
        public int NoticeId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Target Audience is required")]
        [StringLength(100)]
        public string TargetAudience { get; set; } = string.Empty;

        [StringLength(500)]
        public string? AttachmentPath { get; set; }

        public bool IsPinned { get; set; } = false;

        [Required]
        public DateTime ValidFrom { get; set; } = DateTime.Today;

        public DateTime? ValidTill { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
