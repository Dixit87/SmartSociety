using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class NoticeController : Controller
    {
        private readonly INoticeRepository _noticeRepository;
        private readonly IPollRepository _pollRepository;
        private readonly IWebHostEnvironment _env;
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserRepository _userRepo;

        public NoticeController(
            INoticeRepository noticeRepository, 
            IPollRepository pollRepository, 
            IWebHostEnvironment env,
            INotificationRepository notificationRepo,
            IUserRepository userRepo)
        {
            _noticeRepository = noticeRepository;
            _pollRepository = pollRepository;
            _env = env;
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveNotice(Notice notice, IFormFile? attachmentFile)
        {
            try
            {
                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(attachmentFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Allowed attachment formats: PDF, Word, Excel, Images." });
                    }

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

                bool isNew = notice.NoticeId == 0;
                await _noticeRepository.UpsertNoticeAsync(notice);

                if (isNew)
                {
                    var residents = (await _userRepo.GetAllUsersAsync())
                        .Where(u => string.Equals(u.Role, "Resident", StringComparison.OrdinalIgnoreCase) && u.IsActive);
                    
                    foreach (var resident in residents)
                    {
                        await _notificationRepo.InsertAsync(new Notification
                        {
                            UserId = resident.UserId,
                            Title = "New Announcement: " + notice.Title,
                            Message = notice.Description.Length > 150 ? notice.Description.Substring(0, 150) + "..." : notice.Description,
                            Category = "Notice"
                        });
                    }
                }

                return Json(new { success = true, message = "Notice saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving the notice." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        [HttpGet]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> Board()
        {
            var notices = await _noticeRepository.GetAllNoticesAsync("Active");
            var polls = await _pollRepository.GetAllPollsAsync();

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userVotes = new Dictionary<int, int>();
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int userId))
            {
                userVotes = await _pollRepository.GetUserVotesAsync(userId);
            }

            System.Dynamic.ExpandoObject model = new System.Dynamic.ExpandoObject();
            dynamic dModel = model;
            dModel.Notices = notices.OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.CreatedAt);
            dModel.Polls = polls;
            dModel.UserVotes = userVotes;

            return View(model);
        }
    }
}
