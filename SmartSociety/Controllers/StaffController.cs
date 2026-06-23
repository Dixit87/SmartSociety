using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    public class StaffController : Controller
    {
        private readonly IStaffRepository _staffRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StaffController(IStaffRepository staffRepo, IFlatRepository flatRepo, IWebHostEnvironment webHostEnvironment)
        {
            _staffRepo = staffRepo;
            _flatRepo = flatRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var role = Request.Cookies["MockRole"];
            if (role != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            var staffList = await _staffRepo.GetAllStaffAsync();
            foreach(var staff in staffList)
            {
                staff.AssignedFlatIds = (await _staffRepo.GetAssignedFlatsAsync(staff.StaffId)).ToList();
            }

            ViewBag.Flats = await _flatRepo.GetAllFlatsAsync();
            return View(staffList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveStaff(Staff staff, List<int> assignedFlatIds)
        {
            try
            {
                if (staff.PhotoFile != null && staff.PhotoFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "staff");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + staff.PhotoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await staff.PhotoFile.CopyToAsync(fileStream);
                    }
                    staff.PhotoPath = "/uploads/staff/" + uniqueFileName;
                }

                int newStaffId = await _staffRepo.SaveStaffAsync(staff);
                
                // Handle flat assignments
                string flatIdsStr = assignedFlatIds != null && assignedFlatIds.Count > 0 ? string.Join(",", assignedFlatIds) : "";
                await _staffRepo.AssignFlatsAsync(newStaffId, flatIdsStr);

                return Json(new { success = true, message = "Staff details saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving staff: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                await _staffRepo.ToggleStatusAsync(id);
                return Json(new { success = true, message = "Status updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating status: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogAttendance(int id, string logType)
        {
            try
            {
                await _staffRepo.LogAttendanceAsync(id, logType);
                return Json(new { success = true, message = $"Attendance logged ({logType}) successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error logging attendance: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceHistory(int id)
        {
            try
            {
                var history = await _staffRepo.GetAttendanceHistoryAsync(id);
                return Json(new { success = true, data = history });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error fetching attendance: " + ex.Message });
            }
        }
    }
}
