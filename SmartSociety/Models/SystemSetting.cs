using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class SystemSetting
    {
        public int SettingId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Society Name")]
        public string SocietyName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Registration Number")]
        public string? RegistrationNo { get; set; }

        [StringLength(500)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Phone]
        [StringLength(50)]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Currency Symbol")]
        public string CurrencySymbol { get; set; } = "₹";

        [Required]
        [Range(0, 100)]
        [Display(Name = "Default Penalty (%)")]
        public decimal DefaultPenaltyPercentage { get; set; } = 5.0m;
    }

    public class DatabaseBackup
    {
        public int BackupId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public decimal SizeMB { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SettingsViewModel
    {
        public SystemSetting Settings { get; set; } = new SystemSetting();
        public List<DatabaseBackup> Backups { get; set; } = new List<DatabaseBackup>();
    }
}
