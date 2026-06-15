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

namespace SmartSociety.Controllers
{
    public class ComplaintController : Controller
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly IUserRepository _userRepo;
        private readonly IWebHostEnvironment _env;

        public ComplaintController(IComplaintRepository complaintRepo, IFlatRepository flatRepo, IUserRepository userRepo, IWebHostEnvironment env)
        {
            _complaintRepo = complaintRepo;
            _flatRepo = flatRepo;
            _userRepo = userRepo;
            _env = env;
        }

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
        public async Task<IActionResult> Create(int flatId, int raisedBy, string category, string title, string description, string priority, IFormFile photo)
        {
            try
            {
                string photoUrl = null;
                if (photo != null && photo.Length > 0)
                {
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
        public async Task<IActionResult> UpdateStatus(int complaintId, string status, string adminRemarks)
        {
            try
            {
                await _complaintRepo.UpdateStatusAsync(complaintId, status, adminRemarks);
                return Json(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update status: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int complaintId, int assignedTo)
        {
            try
            {
                await _complaintRepo.AssignAsync(complaintId, assignedTo);
                return Json(new { success = true, message = "Complaint assigned successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to assign complaint: " + ex.Message });
            }
        }
    }
}
