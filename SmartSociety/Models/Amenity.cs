using System;

namespace SmartSociety.Models
{
    public class Amenity
    {
        public int AmenityId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AmenityBooking
    {
        public int BookingId { get; set; }
        public int AmenityId { get; set; }
        public int FlatId { get; set; }
        public int UserId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Purpose { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Joined Properties
        public string AmenityName { get; set; }
        public string FlatNo { get; set; }
        public string ResidentName { get; set; }
        public string ResidentPhone { get; set; }
    }
}
