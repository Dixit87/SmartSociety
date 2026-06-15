using System.ComponentModel.DataAnnotations;

namespace SmartSociety.Models
{
    public class MaintenanceSetting
    {
        public int SettingId { get; set; }

        [Required]
        [Display(Name = "Billing Type")]
        public string BillingType { get; set; } = "Fixed"; // "Fixed" or "PerSqFt"

        [Required]
        [Display(Name = "Rate / Amount")]
        public decimal Rate { get; set; }

        [Required]
        [Display(Name = "Penalty Amount")]
        public decimal PenaltyAmount { get; set; }

        [Required]
        [Display(Name = "Days until Due")]
        public int DueDays { get; set; }

        public DateTime UpdatedAt { get; set; }
    }

    public class MaintenanceBill
    {
        public int BillId { get; set; }
        public int FlatId { get; set; }
        
        [Display(Name = "Billing Month")]
        public int BillMonth { get; set; }
        
        [Display(Name = "Billing Year")]
        public int BillYear { get; set; }
        
        [Display(Name = "Base Amount")]
        public decimal BaseAmount { get; set; }
        
        [Display(Name = "Extra Charges")]
        public decimal ExtraCharges { get; set; }
        
        [Display(Name = "Remarks")]
        public string? ExtraChargeRemarks { get; set; }
        
        [Display(Name = "Penalty")]
        public decimal PenaltyAmount { get; set; }
        
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }
        
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }
        
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }
        
        public string Status { get; set; } = "Unpaid";
        public DateTime CreatedAt { get; set; }

        // Navigation Properties (populated by SP)
        public string? FlatNumber { get; set; }
        public string? BlockName { get; set; }
        public string? OwnerName { get; set; }
        public string? TenantName { get; set; }

        public decimal BalanceAmount => TotalAmount - AmountPaid;
        public string DisplayMonthYear => new DateTime(BillYear, BillMonth, 1).ToString("MMM yyyy");
    }

    public class BillPayment
    {
        public int PaymentId { get; set; }
        public int BillId { get; set; }
        
        [Required]
        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; }
        
        public DateTime PaymentDate { get; set; }
        
        [Required]
        [Display(Name = "Payment Mode")]
        public string PaymentMode { get; set; } = "Cash"; // Cash, Cheque, UPI, BankTransfer
        
        [Display(Name = "Transaction ID")]
        public string? TransactionId { get; set; }
        
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MaintenanceDashboardStats
    {
        public decimal TotalExpectedThisMonth { get; set; }
        public decimal TotalCollectedThisMonth { get; set; }
        public decimal TotalPendingOverall { get; set; }
        public int TotalDefaulters { get; set; }
    }

    public class MaintenanceReceiptViewModel
    {
        public MaintenanceBill Bill { get; set; } = new MaintenanceBill();
        public List<BillPayment> Payments { get; set; } = new List<BillPayment>();
    }
}
