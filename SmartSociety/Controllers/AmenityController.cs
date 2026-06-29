using System;
using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class AmenityController : Controller
    {
        private readonly IAmenityRepository _amenityRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly IUserRepository _userRepo;

        public AmenityController(IAmenityRepository amenityRepo, IFlatRepository flatRepo, IUserRepository userRepo)
        {
            _amenityRepo = amenityRepo;
            _flatRepo = flatRepo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            var amenities = await _amenityRepo.GetAllAmenitiesAsync();
            var bookings = await _amenityRepo.GetAllBookingsAsync();
            
            // For Add Booking Dropdowns
            var flats = await _flatRepo.GetAllFlatsAsync();
            var users = await _userRepo.GetAllUsersAsync();

            dynamic model = new ExpandoObject();
            model.Amenities = amenities;
            model.Bookings = bookings;
            model.Flats = flats;
            model.Users = users;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveAmenity(int amenityId, string name, string description, int capacity, TimeSpan openTime, TimeSpan closeTime, decimal pricePerHour, bool isActive)
        {
            try
            {
                var amenity = new Amenity
                {
                    AmenityId = amenityId,
                    Name = name,
                    Description = description,
                    Capacity = capacity,
                    OpenTime = openTime,
                    CloseTime = closeTime,
                    PricePerHour = pricePerHour,
                    IsActive = isActive
                };

                if (amenityId == 0)
                {
                    await _amenityRepo.CreateAmenityAsync(amenity);
                    return Json(new { success = true, message = "Facility added successfully." });
                }
                else
                {
                    await _amenityRepo.UpdateAmenityAsync(amenity);
                    return Json(new { success = true, message = "Facility updated successfully." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to save facility: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAmenity(int amenityId)
        {
            try
            {
                await _amenityRepo.DeleteAmenityAsync(amenityId);
                return Json(new { success = true, message = "Facility removed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to remove facility: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBooking(int amenityId, int flatId, int userId, DateTime bookingDate, TimeSpan startTime, TimeSpan endTime, string purpose, decimal totalAmount, string paymentStatus)
        {
            try
            {
                var booking = new AmenityBooking
                {
                    AmenityId = amenityId,
                    FlatId = flatId,
                    UserId = userId,
                    BookingDate = bookingDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    Purpose = purpose,
                    TotalAmount = totalAmount,
                    PaymentStatus = paymentStatus,
                    Status = "Approved" // Admins creating it directly gets auto-approved.
                };

                await _amenityRepo.CreateBookingAsync(booking);
                return Json(new { success = true, message = "Booking created successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to create booking: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status, string remarks)
        {
            try
            {
                await _amenityRepo.UpdateBookingStatusAsync(bookingId, status, remarks);
                return Json(new { success = true, message = $"Booking {status.ToLower()} successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update booking status: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> MyBookings()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    var bookings = await _amenityRepo.GetBookingsByFlatIdAsync(flat.FlatId);
                    var amenities = await _amenityRepo.GetAllAmenitiesAsync();
                    
                    dynamic model = new ExpandoObject();
                    model.Bookings = bookings;
                    model.Amenities = amenities.Where(a => a.IsActive);
                    model.Flat = flat;
                    model.UserId = userId;

                    return View(model);
                }
            }
            return View(new ExpandoObject());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> BookAmenity(int amenityId, DateTime bookingDate, TimeSpan startTime, TimeSpan endTime, string purpose)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                    if (flat == null)
                    {
                        return Json(new { success = false, message = "You do not have a flat assigned to book amenities." });
                    }

                    var amenities = await _amenityRepo.GetAllAmenitiesAsync();
                    var amenity = amenities.FirstOrDefault(a => a.AmenityId == amenityId);
                    if (amenity == null || !amenity.IsActive)
                    {
                        return Json(new { success = false, message = "Selected amenity is not active or available." });
                    }

                    decimal totalAmount = 0;
                    if (amenity.PricePerHour > 0)
                    {
                        double durationHours = (endTime - startTime).TotalHours;
                        if (durationHours <= 0)
                        {
                            return Json(new { success = false, message = "End time must be after start time." });
                        }
                        totalAmount = amenity.PricePerHour * (decimal)durationHours;
                    }

                    var booking = new AmenityBooking
                    {
                        AmenityId = amenityId,
                        FlatId = flat.FlatId,
                        UserId = userId,
                        BookingDate = bookingDate,
                        StartTime = startTime,
                        EndTime = endTime,
                        Purpose = purpose,
                        TotalAmount = totalAmount,
                        PaymentStatus = amenity.PricePerHour > 0 ? "Pending" : "Paid",
                        Status = "Pending"
                    };

                    int bookingId = await _amenityRepo.CreateBookingAsync(booking);
                    return Json(new { success = true, bookingId = bookingId, totalAmount = totalAmount, message = "Booking request submitted successfully." });
                }
                return Json(new { success = false, message = "Unauthorized access." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to book amenity: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> RecordBookingPayment(int bookingId, string paymentMode, string transactionId)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                    if (flat == null) return Json(new { success = false, message = "Flat not found." });

                    await _amenityRepo.UpdateBookingPaymentStatusAsync(bookingId, "Paid");
                    return Json(new { success = true, message = "Booking payment recorded successfully." });
                }
                return Json(new { success = false, message = "Unauthorized access." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to record payment: " + ex.Message });
            }
        }
    }
}
