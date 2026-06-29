using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
    public class ClassifiedsController : Controller
    {
        private readonly IClassifiedsRepository _classifiedsRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClassifiedsController(IClassifiedsRepository classifiedsRepo, IFlatRepository flatRepo, IWebHostEnvironment webHostEnvironment)
        {
            _classifiedsRepo = classifiedsRepo;
            _flatRepo = flatRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category, string? type)
        {
            var ads = await _classifiedsRepo.GetAllActiveAdsAsync();

            if (!string.IsNullOrEmpty(category))
            {
                ads = ads.Where(a => string.Equals(a.AdCategory, category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(type))
            {
                ads = ads.Where(a => string.Equals(a.AdType, type, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.SelectedCategory = category;
            ViewBag.SelectedType = type;

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                ViewBag.CurrentUserId = userId;
            }

            return View(ads);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAd(ClassifiedAd ad)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                if (string.IsNullOrWhiteSpace(ad.Title) || string.IsNullOrWhiteSpace(ad.Description) || ad.Price <= 0 || string.IsNullOrWhiteSpace(ad.AdCategory) || string.IsNullOrWhiteSpace(ad.AdType))
                {
                    return Json(new { success = false, message = "All required fields must be completed." });
                }

                ad.UserId = userId;
                ad.IsActive = true;

                if (ad.ImageFile != null && ad.ImageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(ad.ImageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Invalid image format. Only .jpg, .jpeg, .png, .webp are allowed." });
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "classifieds");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ad.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ad.ImageFile.CopyToAsync(fileStream);
                    }

                    ad.ImagePath = "/uploads/classifieds/" + uniqueFileName;
                }

                await _classifiedsRepo.SaveAdAsync(ad);
                return Json(new { success = true, message = "Classified advertisement posted successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error posting ad: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAd(int id)
        {
            try
            {
                var ad = await _classifiedsRepo.GetAdByIdAsync(id);
                if (ad == null)
                {
                    return Json(new { success = false, message = "Ad not found." });
                }

                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (!int.TryParse(userIdStr, out int userId) || (ad.UserId != userId && role != "Admin"))
                {
                    return Json(new { success = false, message = "Unauthorized to delete this advertisement." });
                }

                // Delete physical file if it exists
                if (!string.IsNullOrEmpty(ad.ImagePath))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, ad.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                await _classifiedsRepo.DeleteAdAsync(id);
                return Json(new { success = true, message = "Advertisement deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting ad: " + ex.Message });
            }
        }
    }
}
