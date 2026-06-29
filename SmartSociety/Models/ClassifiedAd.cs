using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSociety.Models
{
    public class ClassifiedAd
    {
        public int ClassifiedId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 4000 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000000.00, ErrorMessage = "Price must be a positive number.")]
        public decimal Price { get; set; }

        [Required]
        public string AdCategory { get; set; } = string.Empty; // e.g. Electronics, Furniture, Vehicles, Other

        [Required]
        public string AdType { get; set; } = string.Empty; // e.g. Sell, Rent

        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Joined properties
        public string? OwnerName { get; set; }
        public string? OwnerPhone { get; set; }
        public string? OwnerFlatNumber { get; set; }

        // Form file upload
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}
