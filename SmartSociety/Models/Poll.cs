using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Poll
    {
        public int PollId { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Question { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        public string Status { get; set; } = "Active";
        
        public DateTime CreatedAt { get; set; }
        
        // Joined field
        public int TotalVotes { get; set; }
        
        // Associated Options
        public List<PollOption> Options { get; set; } = new List<PollOption>();
    }

    public class PollOption
    {
        public int OptionId { get; set; }
        
        public int PollId { get; set; }
        
        [Required]
        [StringLength(250)]
        public string OptionText { get; set; } = null!;
        
        // Joined field for results
        public int VoteCount { get; set; }
        
        // Calculated field for UI
        public double VotePercentage { get; set; }
    }

    // View Model for creating polls from the frontend
    public class PollUpsertViewModel
    {
        public int PollId { get; set; }
        
        [Required]
        public string Question { get; set; } = null!;
        
        public string? Description { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        // Array of option strings posted from the dynamic form
        public List<string> Options { get; set; } = new List<string>();
    }
}
