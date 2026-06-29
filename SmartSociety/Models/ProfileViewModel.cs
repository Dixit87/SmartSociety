using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class ProfileViewModel
    {
        public User UserInfo { get; set; } = new User();

        public List<FamilyMember> FamilyMembers { get; set; } = new List<FamilyMember>();

        // Password change fields
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long.")]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmPassword { get; set; }
    }
}
