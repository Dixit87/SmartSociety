using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class ForumReply
    {
        public int ReplyId { get; set; }

        [Required]
        public int TopicId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 2, ErrorMessage = "Reply content must be between 2 and 4000 characters.")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Joined properties
        public string? AuthorName { get; set; }
        public string? AuthorFlat { get; set; }
    }
}
