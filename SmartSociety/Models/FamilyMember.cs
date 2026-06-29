using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class FamilyMember
    {
        public int FamilyMemberId { get; set; }
        
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relation is required.")]
        [StringLength(50)]
        public string Relation { get; set; } = string.Empty; // Spouse, Child, Parent, Sibling, Other

        [Phone(ErrorMessage = "Invalid Phone Number.")]
        [StringLength(20, ErrorMessage = "Phone Number cannot exceed 20 characters.")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
