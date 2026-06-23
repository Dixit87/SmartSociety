using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class MessageLog
    {
        public int MessageId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string MessageType { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string Audience { get; set; } = null!;
        
        [StringLength(250)]
        public string? Subject { get; set; }
        
        [Required]
        public string Body { get; set; } = null!;
        
        public DateTime SentAt { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Sent";
    }
}
