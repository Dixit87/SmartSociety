using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class User
    {
        public int UserId { get; set; }
        
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [StringLength(100)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(255)]
        public string? PasswordHash { get; set; }

        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Flat Number")]
        public string? FlatNumber { get; set; }

        [StringLength(255)]
        public string? ProfilePicture { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
