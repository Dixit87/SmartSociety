using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Block
    {
        public int BlockId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Block Name")]
        public string BlockName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Total Floors")]
        [Range(1, 200, ErrorMessage = "Total Floors must be between 1 and 200")]
        public int TotalFloors { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
