using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.IO;

namespace SmartSociety.Controllers
{
    public class NoticeController : Controller
    {
        private readonly INoticeRepository _noticeRepository;
        private readonly IWebHostEnvironment _env;

        public NoticeController(INoticeRepository noticeRepository, IWebHostEnvironment env)
        {
            _noticeRepository = noticeRepository;
            _env = env;
        }

        public async Task<IActionResult> Index(string? status, string? category)
        {
            var notices = await _noticeRepository.GetAllNoticesAsync(status, category);
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentCategory = category;

            // Compute some stats
            var allNotices = await _noticeRepository.GetAllNoticesAsync();
            ViewBag.TotalNotices = allNotices.Count();
            ViewBag.ActiveNotices = allNotices.Count(x => x.Status == "Active");
            ViewBag.PinnedNotices = allNotices.Count(x => x.IsPinned && x.Status == "Active");

            return View(notices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNotice(Notice notice, IFormFile? attachmentFile)
        {
            try
            {
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "notices");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(attachmentFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await attachmentFile.CopyToAsync(fileStream);
                    }

                    notice.AttachmentPath = "/uploads/notices/" + uniqueFileName;
                }

                // Default CreatedBy for mockup
                notice.CreatedBy = 1; 

                await _noticeRepository.UpsertNoticeAsync(notice);

                return Json(new { success = true, message = "Notice saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving the notice." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotice(int noticeId)
        {
            try
            {
                await _noticeRepository.DeleteNoticeAsync(noticeId);
                return Json(new { success = true, message = "Notice deleted successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting notice." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePin(int noticeId, bool isPinned)
        {
            try
            {
                await _noticeRepository.TogglePinStatusAsync(noticeId, isPinned);
                return Json(new { success = true, message = isPinned ? "Notice pinned to top!" : "Notice unpinned." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error toggling pin status." });
            }
        }
    }
}
