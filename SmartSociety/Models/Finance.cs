using System;
using System.Collections.Generic;

namespace SmartSociety.Models
{
    public class ExpenseCategory
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool IsActive { get; set; }
    }

    public class Expense
    {
        public int ExpenseId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateIncurred { get; set; }
        public string PaidTo { get; set; }
        public string PaymentMethod { get; set; }
        public string ReferenceNo { get; set; }
        public string ReceiptUrl { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OtherIncome
    {
        public int IncomeId { get; set; }
        public string Source { get; set; }
        public string Title { get; set; }
        public decimal Amount { get; set; }
        public DateTime DateReceived { get; set; }
        public string ReceivedFrom { get; set; }
        public string PaymentMethod { get; set; }
        public string ReferenceNo { get; set; }
        public string ReceiptUrl { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FinanceDashboardStats
    {
        public decimal TotalMaintenanceIncome { get; set; }
        public decimal TotalUtilityIncome { get; set; }
        public decimal TotalOtherIncome { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; }
    }

    public class ChartDataPoint
    {
        public string CategoryName { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class FixedDeposit
    {
        public int FdId { get; set; }
        public string FdNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; }
        public DateTime MaturityDate { get; set; }
        public decimal MaturityAmount { get; set; }
        public string Status { get; set; } = "Active"; // Active, Matured, Liquidated
        public DateTime DateInvested { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }

    public class SinkingFundTransaction
    {
        public int TransactionId { get; set; }
        public string Type { get; set; } = "Contribution"; // Contribution, Withdrawal
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string Purpose { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
    }
}
