using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartSociety.Models
{
    public class SocietyDocument
    {
        public int DocumentId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = null!;
        
        public string FilePath { get; set; } = null!;
        
        public DateTime UploadedAt { get; set; }
        
        public bool IsVisibleToResidents { get; set; }
    }

    public class DocumentUploadViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = null!;
        
        public bool IsVisibleToResidents { get; set; }
        
        [Required]
        public IFormFile File { get; set; } = null!;
    }

    public class DocumentEditViewModel
    {
        public int DocumentId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = null!;
        
        public bool IsVisibleToResidents { get; set; }
        
        // Optional file for replacement
        public IFormFile? File { get; set; }
    }
}
