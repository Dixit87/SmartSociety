using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;
using SmartSociety.Models;
using SmartSociety.Repositories;

namespace SmartSociety.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserRepository _userRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(IUserRepository userRepo, IWebHostEnvironment webHostEnvironment)
        {
            _userRepo = userRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepo.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            User model = new User();

            if (id.HasValue && id.Value > 0)
            {
                var user = await _userRepo.GetUserByIdAsync(id.Value);
                if (user == null)
                {
                    return NotFound();
                }
                model = user;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(User model, IFormFile? profileImage)
        {
            // Custom validation for password
            if (model.UserId == 0 && string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required for new users.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Hash password if provided
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        model.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    }
                    if (profileImage != null && profileImage.Length > 0)
                    {
                        // Delete old image if updating
                        if (model.UserId > 0)
                        {
                            var existingUser = await _userRepo.GetUserByIdAsync(model.UserId);
                            if (existingUser != null && !string.IsNullOrEmpty(existingUser.ProfilePicture))
                            {
                                string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingUser.ProfilePicture.TrimStart('/'));
                                if (System.IO.File.Exists(oldImagePath))
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                            }
                        }

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + profileImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profileImage.CopyToAsync(fileStream);
                        }

                        model.ProfilePicture = "/uploads/profiles/" + uniqueFileName;
                    }
                    await _userRepo.UpsertUserAsync(model);
                    TempData["SuccessMessage"] = model.UserId == 0 ? "User created successfully." : "User updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log exception here in a real scenario
                    ModelState.AddModelError("", "An error occurred while saving the user: " + ex.Message);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters long." });
                }

                var user = await _userRepo.GetUserByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                await _userRepo.UpsertUserAsync(user);

                return Json(new { success = true, message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error resetting password: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(id);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                user.IsActive = !user.IsActive; // Toggle the status
                await _userRepo.UpsertUserAsync(user);

                return Json(new { success = true, message = $"User status updated to {(user.IsActive ? "Active" : "Inactive")}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating user status: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(id);
                if (user != null && !string.IsNullOrEmpty(user.ProfilePicture))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                await _userRepo.DeleteUserAsync(id);
                return Json(new { success = true, message = "User deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting user: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Users");
                
                // Headers (Removed Role as it will be hardcoded to Resident)
                worksheet.Cell(1, 1).Value = "FullName";
                worksheet.Cell(1, 2).Value = "PhoneNumber";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "FlatNumber";
                worksheet.Cell(1, 5).Value = "Password";
                
                // Make headers bold
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "User_Bulk_Template.xlsx");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(IFormFile uploadFile)
        {
            if (uploadFile == null || uploadFile.Length == 0)
            {
                return Json(new { success = false, message = "Please select a valid Excel file." });
            }

            if (!uploadFile.FileName.EndsWith(".xlsx"))
            {
                return Json(new { success = false, message = "Only .xlsx files are supported." });
            }

            int successCount = 0;
            int errorCount = 0;

            try
            {
                using (var stream = new MemoryStream())
                {
                    await uploadFile.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header row

                        foreach (var row in rows)
                        {
                            try
                            {
                                string fullName = row.Cell(1).GetString().Trim();
                                string phone = row.Cell(2).GetString().Trim();
                                string email = row.Cell(3).GetString().Trim();
                                string flatNo = row.Cell(4).GetString().Trim();
                                string password = row.Cell(5).GetString().Trim();

                                if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(password))
                                {
                                    errorCount++;
                                    continue;
                                }

                                var user = new User
                                {
                                    FullName = fullName,
                                    PhoneNumber = phone,
                                    Email = email,
                                    Role = "Resident", // Hardcoded per user request
                                    FlatNumber = flatNo,
                                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                                    IsActive = true
                                };

                                await _userRepo.UpsertUserAsync(user);
                                successCount++;
                            }
                            catch
                            {
                                errorCount++;
                            }
                        }
                    }
                }

                string msg = $"Bulk upload completed. {successCount} users imported successfully.";
                if (errorCount > 0)
                {
                    msg += $" {errorCount} rows failed or had missing required fields.";
                }

                return Json(new { success = true, message = msg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing file: " + ex.Message });
            }
        }
    }
}
