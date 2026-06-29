using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserRepository _userRepo;

        public NotificationController(INotificationRepository notificationRepo, IUserRepository userRepo)
        {
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var notifications = await _notificationRepo.GetByUserIdAsync(userId);
            
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                notifications = notifications.Where(n => string.Equals(n.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.CurrentCategory = category ?? "All";
            
            // Stats
            var allNotifications = await _notificationRepo.GetByUserIdAsync(userId);
            ViewBag.TotalCount = allNotifications.Count();
            ViewBag.UnreadCount = allNotifications.Count(n => !n.IsRead);

            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationRepo.MarkAsReadAsync(id);
                return Json(new { success = true, message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    await _notificationRepo.MarkAllAsReadAsync(userId);
                    return Json(new { success = true, message = "All notifications marked as read." });
                }
                return Json(new { success = false, message = "User not identified." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _notificationRepo.DeleteAsync(id);
                return Json(new { success = true, message = "Notification deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var notifications = await _notificationRepo.GetByUserIdAsync(userId);
                    int count = notifications.Count(n => !n.IsRead);
                    return Json(new { success = true, count = count });
                }
                return Json(new { success = false, count = 0 });
            }
            catch
            {
                return Json(new { success = false, count = 0 });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> BroadcastEmergency(string alertType, string message)
        {
            try
            {
                var residents = (await _userRepo.GetAllUsersAsync())
                    .Where(u => string.Equals(u.Role, "Resident", StringComparison.OrdinalIgnoreCase) && u.IsActive);

                string title = $"🚨 EMERGENCY: {alertType.ToUpper()}";

                foreach (var resident in residents)
                {
                    await _notificationRepo.InsertAsync(new Notification
                    {
                        UserId = resident.UserId,
                        Title = title,
                        Message = message,
                        Category = "General"
                    });
                }

                return Json(new { success = true, message = $"Emergency {alertType} alert broadcasted to all residents!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to broadcast: " + ex.Message });
            }
        }
    }
}
