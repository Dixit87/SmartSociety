using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class InventoryItem
    {
        public int ItemId { get; set; }
        
        [Required]
        [StringLength(150)]
        public string ItemName { get; set; } = null!;
        
        [Required]
        [Range(0, 100000)]
        public int Quantity { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Unit { get; set; } = "pcs";
        
        [Required]
        [Range(0, 1000000)]
        public decimal CostPerUnit { get; set; }
        
        [Required]
        [Range(0, 1000)]
        public int MinStockLevel { get; set; } = 5;
        
        public DateTime UpdatedAt { get; set; }
    }

    public class ComplaintSparePart
    {
        public int ComplaintId { get; set; }
        public int ItemId { get; set; }
        public string? ItemName { get; set; } // Joined field
        
        [Required]
        [Range(1, 1000)]
        public int QuantityUsed { get; set; }
        
        [Required]
        public decimal CostPerUnit { get; set; }
        
        public decimal TotalCost => QuantityUsed * CostPerUnit;
        
        public DateTime CreatedAt { get; set; }
    }
}
