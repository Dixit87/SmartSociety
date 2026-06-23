using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class AuditLog
    {
        public int LogId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty; // Create, Update, Delete, Login
        
        [Required]
        [StringLength(100)]
        public string ModuleName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; }
        
        [StringLength(50)]
        public string? IPAddress { get; set; }
    }
}
