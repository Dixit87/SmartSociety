using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class UtilityType
    {
        public int UtilityTypeId { get; set; }
        
        [Required]
        [Display(Name = "Utility Name")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Rate Per Unit (₹)")]
        public decimal RatePerUnit { get; set; }
        
        [Required]
        [Display(Name = "Measurement Unit")]
        public string MeasurementUnit { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    public class UtilityBill
    {
        public int BillId { get; set; }
        
        [Required]
        [Display(Name = "Flat")]
        public int FlatId { get; set; }
        
        [Required]
        [Display(Name = "Utility Type")]
        public int UtilityTypeId { get; set; }
        
        [Required]
        [Display(Name = "Billing Month")]
        public int BillMonth { get; set; }
        
        [Required]
        [Display(Name = "Billing Year")]
        public int BillYear { get; set; }
        
        [Display(Name = "Previous Reading")]
        public decimal PreviousReading { get; set; }
        
        [Required]
        [Display(Name = "Current Reading")]
        public decimal CurrentReading { get; set; }
        
        public decimal ConsumedUnits { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "Unpaid";
        public DateTime CreatedAt { get; set; }

        // Navigation / JOIN Properties
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
        public string? OwnerPhone { get; set; }
        public string? TenantName { get; set; }
        public string? UtilityName { get; set; }
        public decimal RatePerUnit { get; set; }
        public string? MeasurementUnit { get; set; }

        public decimal BalanceAmount => TotalAmount - AmountPaid;
        public string DisplayMonthYear => new DateTime(BillYear, BillMonth, 1).ToString("MMM yyyy");
    }

    public class UtilityPayment
    {
        public int PaymentId { get; set; }
        public int BillId { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMode { get; set; } = "Cash";
        public string? TransactionId { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UtilityDashboardStats
    {
        public decimal TotalBilledThisMonth { get; set; }
        public decimal TotalCollectedThisMonth { get; set; }
        public decimal TotalPendingOverall { get; set; }
        public int DefaultersCount { get; set; }
    }

    public class UtilityReceiptViewModel
    {
        public UtilityBill Bill { get; set; } = new UtilityBill();
        public IEnumerable<UtilityPayment> Payments { get; set; } = new List<UtilityPayment>();
    }
}
