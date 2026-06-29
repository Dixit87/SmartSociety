using System;

namespace SmartSociety.Models
{
    public class SmartMeterLog
    {
        public int LogId { get; set; }
        public int MeterId { get; set; }
        public decimal UnitsConsumed { get; set; }
        public decimal Cost { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
