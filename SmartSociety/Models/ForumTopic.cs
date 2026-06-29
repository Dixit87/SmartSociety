using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class ForumTopic
    {
        public int TopicId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(8000, MinimumLength = 10, ErrorMessage = "Content must be between 10 and 8000 characters.")]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = "General"; // e.g. General, Events, Issues, Suggestions

        public bool IsPinned { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Joined properties
        public string? AuthorName { get; set; }
        public string? AuthorFlat { get; set; }
        public int ReplyCount { get; set; }
    }
}
