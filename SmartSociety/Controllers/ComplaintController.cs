using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class ComplaintController : Controller
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly IUserRepository _userRepo;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationRepository _notificationRepo;
        private readonly IAssetVendorRepository _assetVendorRepo;

        public ComplaintController(
            IComplaintRepository complaintRepo, 
            IFlatRepository flatRepo, 
            IUserRepository userRepo, 
            IWebHostEnvironment env,
            INotificationRepository notificationRepo,
            IAssetVendorRepository assetVendorRepo)
        {
            _complaintRepo = complaintRepo;
            _flatRepo = flatRepo;
            _userRepo = userRepo;
            _env = env;
            _notificationRepo = notificationRepo;
            _assetVendorRepo = assetVendorRepo;
        }

        [Authorize(Roles = "Admin,Technician")]
        public async Task<IActionResult> Index(string status = null, int? month = null, int? year = null)
        {
            var complaints = await _complaintRepo.GetAllAsync(status, month, year);
            var stats = await _complaintRepo.GetDashboardStatsAsync();
            var flats = await _flatRepo.GetAllFlatsAsync();
            var residents = (await _userRepo.GetAllUsersAsync()).Where(u => u.Role == "Resident").ToList();
            var technicians = (await _userRepo.GetAllUsersAsync()).Where(u => u.Role == "Technician" || u.Role == "Admin").ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentMonth = month;
            ViewBag.CurrentYear = year;

            dynamic model = new ExpandoObject();
            model.Complaints = complaints;
            model.Stats = stats;
            model.Flats = flats;
            model.Residents = residents;
            model.Technicians = technicians;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Resident")]
        public async Task<IActionResult> Create(int flatId, int raisedBy, string category, string title, string description, string priority, IFormFile photo)
        {
            try
            {
                if (User.IsInRole("Resident"))
                {
                    var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                        if (flat == null)
                        {
                            return Json(new { success = false, message = "You do not have a flat assigned to raise complaints." });
                        }
                        flatId = flat.FlatId;
                        raisedBy = userId;
                    }
                    else
                    {
                        return Json(new { success = false, message = "Unauthorized access." });
                    }
                }
                string photoUrl = null;
                if (photo != null && photo.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(photo.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only image files (.jpg, .jpeg, .png) are allowed." });
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "complaints");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(fileStream);
                    }
                    photoUrl = "/uploads/complaints/" + uniqueFileName;
                }

                var complaint = new Complaint
                {
                    FlatId = flatId,
                    RaisedBy = raisedBy,
                    Category = category,
                    Title = title,
                    Description = description,
                    Priority = priority,
                    PhotoUrl = photoUrl
                };

                await _complaintRepo.CreateAsync(complaint);
                return Json(new { success = true, message = "Complaint registered successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to register complaint: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Resident")]
        public async Task<IActionResult> Edit(int complaintId, string category, string title, string description, string priority)
        {
            try
            {
                var complaint = new Complaint
                {
                    ComplaintId = complaintId,
                    Category = category,
                    Title = title,
                    Description = description,
                    Priority = priority
                };

                await _complaintRepo.UpdateAsync(complaint);
                return Json(new { success = true, message = "Complaint updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update complaint: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int complaintId)
        {
            try
            {
                var existing = await _complaintRepo.GetByIdAsync(complaintId);
                if (existing != null && !string.IsNullOrEmpty(existing.PhotoUrl))
                {
                    string filePath = Path.Combine(_env.WebRootPath, existing.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                await _complaintRepo.DeleteAsync(complaintId);
                return Json(new { success = true, message = "Complaint deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to delete complaint: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Technician")]
        public async Task<IActionResult> UpdateStatus(int complaintId, string status, string adminRemarks, string? sparePartsJson = null)
        {
            try
            {
                var complaint = await _complaintRepo.GetByIdAsync(complaintId);
                if (complaint == null)
                {
                    return Json(new { success = false, message = "Complaint not found." });
                }

                // Security: IDOR prevention for Technicians
                var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "Technician")
                {
                    var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (!int.TryParse(userIdStr, out int userId) || complaint.AssignedTo != userId)
                    {
                        return Json(new { success = false, message = "Access Denied: This complaint is not assigned to you." });
                    }
                }

                // Save spare parts and deduct stock if resolved and spare parts are provided
                if (status == "Resolved" && !string.IsNullOrWhiteSpace(sparePartsJson))
                {
                    try
                    {
                        var spareParts = System.Text.Json.JsonSerializer.Deserialize<List<SparePartUsage>>(sparePartsJson);
                        if (spareParts != null && spareParts.Count > 0)
                        {
                            foreach (var part in spareParts)
                            {
                                var item = await _assetVendorRepo.GetInventoryItemByIdAsync(part.ItemId);
                                if (item != null && part.QuantityUsed > 0)
                                {
                                    // Deduct stock in database
                                    await _assetVendorRepo.DeductInventoryStockAsync(part.ItemId, part.QuantityUsed);

                                    // Record spare part log for this complaint
                                    var logPart = new ComplaintSparePart
                                    {
                                        ComplaintId = complaintId,
                                        ItemId = part.ItemId,
                                        QuantityUsed = part.QuantityUsed,
                                        CostPerUnit = item.CostPerUnit
                                    };
                                    await _assetVendorRepo.SaveComplaintSparePartAsync(logPart);
                                }
                            }
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        return Json(new { success = false, message = "Status update failed due to invalid spare parts data: " + jsonEx.Message });
                    }
                }

                await _complaintRepo.UpdateStatusAsync(complaintId, status, adminRemarks);

                // Dispatch notification
                if (complaint != null)
                {
                    await _notificationRepo.InsertAsync(new Notification
                    {
                        UserId = complaint.RaisedBy,
                        Title = $"Complaint Ticket #{complaintId} Updated",
                        Message = $"Your complaint '{complaint.Title}' status has been updated to '{status}'. Remarks: {adminRemarks}",
                        Category = "Complaint"
                    });
                }

                return Json(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update status: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(int complaintId, int assignedTo)
        {
            try
            {
                await _complaintRepo.AssignAsync(complaintId, assignedTo);

                // Dispatch notification
                var complaint = await _complaintRepo.GetByIdAsync(complaintId);
                var technician = await _userRepo.GetUserByIdAsync(assignedTo);
                if (complaint != null)
                {
                    string techName = technician?.FullName ?? "a technician";
                    await _notificationRepo.InsertAsync(new Notification
                    {
                        UserId = complaint.RaisedBy,
                        Title = $"Complaint Ticket #{complaintId} Assigned",
                        Message = $"Your complaint '{complaint.Title}' has been assigned to {techName} for inspection.",
                        Category = "Complaint"
                    });
                }

                return Json(new { success = true, message = "Complaint assigned successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to assign complaint: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> MyComplaints()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    var complaints = await _complaintRepo.GetByFlatIdAsync(flat.FlatId);
                    ViewBag.Flat = flat;
                    ViewBag.UserId = userId;
                    return View(complaints);
                }
            }
            return View(new List<Complaint>());
        }

        [HttpGet]
        public async Task<IActionResult> GetComplaintSpareParts(int complaintId)
        {
            try
            {
                var parts = await _assetVendorRepo.GetSparePartsByComplaintIdAsync(complaintId);
                return Json(new { success = true, data = parts });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching spare parts: " + ex.Message });
            }
        }

        public class SparePartUsage
        {
            public int ItemId { get; set; }
            public int QuantityUsed { get; set; }
        }
    }
}
