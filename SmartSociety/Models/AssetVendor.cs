using System;
using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class Vendor
    {
        public int VendorId { get; set; }
        
        [Required]
        [StringLength(150)]
        public string VendorName { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string ServiceCategory { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string ContactPerson { get; set; } = null!;
        
        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = null!;
        
        [StringLength(100)]
        [EmailAddress]
        public string? Email { get; set; }
        
        [Required]
        public DateTime ContractStartDate { get; set; }
        
        [Required]
        public DateTime ContractEndDate { get; set; }
        
        public decimal? ContractCost { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        
        [StringLength(255)]
        public string? ContractDocumentPath { get; set; }
        
        public int? Rating { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    public class Asset
    {
        public int AssetId { get; set; }
        
        [Required]
        [StringLength(150)]
        public string AssetName { get; set; } = null!;
        
        [Required]
        [StringLength(100)]
        public string AssetType { get; set; } = null!;
        
        [Required]
        [StringLength(150)]
        public string Location { get; set; } = null!;
        
        [Required]
        public DateTime PurchaseDate { get; set; }
        
        public decimal? PurchaseCost { get; set; }
        
        public int? VendorId { get; set; }
        
        // This field is joined from Vendors table in Get procedures
        public string? VendorName { get; set; }
        
        public DateTime? AmcExpiryDate { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Active";
        
        [StringLength(255)]
        public string? InvoiceDocumentPath { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    public class AssetServiceLog
    {
        public int LogId { get; set; }
        
        [Required]
        public int AssetId { get; set; }
        
        public int? VendorId { get; set; }
        
        // Joined field
        public string? VendorName { get; set; }
        
        [Required]
        public DateTime ServiceDate { get; set; }
        
        [Required]
        public string Description { get; set; } = null!;
        
        public decimal? Cost { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
