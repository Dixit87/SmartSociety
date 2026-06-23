using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSociety.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string ContactNumber { get; set; }
        public string AadharNumber { get; set; }
        public string PhotoPath { get; set; }
        
        [NotMapped]
        public IFormFile PhotoFile { get; set; }
        
        public TimeSpan ShiftStart { get; set; }
        public TimeSpan ShiftEnd { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        [NotMapped]
        public List<int> AssignedFlatIds { get; set; } = new List<int>();
    }

    public class StaffAttendance
    {
        public int AttendanceId { get; set; }
        public int StaffId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan? CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
    }
}
