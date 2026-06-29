using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class StaffRating
    {
        public int RatingId { get; set; }

        [Required]
        public int StaffId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review cannot exceed 1000 characters.")]
        public string? Review { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Joined properties
        public string? ReviewerName { get; set; }
        public string? ReviewerFlat { get; set; }
    }
}
