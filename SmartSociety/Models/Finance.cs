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
}
