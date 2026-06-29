using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly IFamilyRepository _familyRepo;
        private readonly IWebHostEnvironment _env;

        public ProfileController(IUserRepository userRepo, IFamilyRepository familyRepo, IWebHostEnvironment env)
        {
            _userRepo = userRepo;
            _familyRepo = familyRepo;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User profile not found.");
            }

            var familyMembers = await _familyRepo.GetByUserIdAsync(userId);

            var model = new ProfileViewModel
            {
                UserInfo = user,
                FamilyMembers = familyMembers.ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model, IFormFile? profileImage)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Challenge();
            }

            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Bind values
            user.Email = model.UserInfo.Email;
            user.PhoneNumber = model.UserInfo.PhoneNumber;

            try
            {
                if (profileImage != null && profileImage.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(profileImage.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        TempData["ErrorMessage"] = "Failed to update profile: Only image files (.jpg, .jpeg, .png) are allowed.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Delete old profile picture if exists
                    if (!string.IsNullOrEmpty(user.ProfilePicture))
                    {
                        string oldImagePath = Path.Combine(_env.WebRootPath, user.ProfilePicture.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileImage.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(fileStream);
                    }

                    user.ProfilePicture = "/uploads/profiles/" + uniqueFileName;
                }

                await _userRepo.UpsertUserAsync(user);
                TempData["SuccessMessage"] = "Profile details updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to update profile: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileViewModel model)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Challenge();
            }

            if (string.IsNullOrEmpty(model.CurrentPassword) || string.IsNullOrEmpty(model.NewPassword))
            {
                TempData["ErrorMessage"] = "Please fill in all password fields.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Verify old password
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "The current password you entered is incorrect.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                await _userRepo.UpsertUserAsync(user);
                TempData["SuccessMessage"] = "Password changed successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to change password: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFamilyMember(FamilyMember familyMember)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, message = "Session expired. Please log in again." });
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Validation errors: " + errors });
            }

            try
            {
                // Force user identity for security
                familyMember.UserId = userId;
                await _familyRepo.UpsertFamilyMemberAsync(familyMember);
                return Json(new { success = true, message = familyMember.FamilyMemberId == 0 ? "Family member added successfully!" : "Family member updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving family member: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFamilyMember(int id)
        {
            try
            {
                await _familyRepo.DeleteFamilyMemberAsync(id);
                return Json(new { success = true, message = "Family member removed successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error removing family member: " + ex.Message });
            }
        }
    }
}
