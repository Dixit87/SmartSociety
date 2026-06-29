using System;

namespace SmartSociety.Models
{
    public class SmartMeterRecharge
    {
        public int RechargeId { get; set; }
        public int MeterId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Card";
        public string? TransactionId { get; set; }
        public DateTime RechargeTime { get; set; } = DateTime.Now;
    }
}
