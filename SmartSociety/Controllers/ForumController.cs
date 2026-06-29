using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System;
using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class ForumController : Controller
    {
        private readonly IForumRepository _forumRepo;
        private readonly string _connectionString;

        public ForumController(IForumRepository forumRepo, IConfiguration configuration)
        {
            _forumRepo = forumRepo;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var topics = await _forumRepo.GetAllTopicsAsync();

            if (!string.IsNullOrEmpty(category))
            {
                topics = topics.Where(t => string.Equals(t.Category, category, StringComparison.OrdinalIgnoreCase));
            }

            ViewBag.SelectedCategory = category;

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                ViewBag.CurrentUserId = userId;
            }

            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            ViewBag.IsAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            return View(topics);
        }

        [HttpGet]
        public async Task<IActionResult> Topic(int id)
        {
            var topic = await _forumRepo.GetTopicByIdAsync(id);
            if (topic == null)
            {
                return NotFound("Discussion thread not found.");
            }

            var replies = await _forumRepo.GetRepliesByTopicIdAsync(id);
            ViewBag.Replies = replies;

            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                ViewBag.CurrentUserId = userId;
            }

            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
            ViewBag.IsAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

            return View(topic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTopic(ForumTopic topic)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                if (string.IsNullOrWhiteSpace(topic.Title) || string.IsNullOrWhiteSpace(topic.Content) || string.IsNullOrWhiteSpace(topic.Category))
                {
                    return Json(new { success = false, message = "Title, content, and category must be completed." });
                }

                topic.UserId = userId;
                
                // Only Admins can pin topics
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // topic.IsPinned bound from form
                }
                else
                {
                    topic.IsPinned = false;
                }

                int topicId = await _forumRepo.SaveTopicAsync(topic);
                return Json(new { success = true, topicId = topicId, message = "Discussion thread created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating thread: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(ForumReply reply)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Session expired. Please log in again." });
                }

                if (reply.TopicId <= 0 || string.IsNullOrWhiteSpace(reply.Content) || reply.Content.Length < 2)
                {
                    return Json(new { success = false, message = "Comment content must be at least 2 characters long." });
                }

                reply.UserId = userId;
                await _forumRepo.InsertReplyAsync(reply);

                return Json(new { success = true, message = "Comment added successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error posting comment: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            try
            {
                var topic = await _forumRepo.GetTopicByIdAsync(id);
                if (topic == null)
                {
                    return Json(new { success = false, message = "Thread not found." });
                }

                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (!int.TryParse(userIdStr, out int userId) || (topic.UserId != userId && role != "Admin"))
                {
                    return Json(new { success = false, message = "Unauthorized to delete this thread." });
                }

                await _forumRepo.DeleteTopicAsync(id);
                return Json(new { success = true, message = "Thread deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting thread: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int id)
        {
            try
            {
                // Query the reply from the database using direct connection to get author
                using var connection = new SqlConnection(_connectionString);
                var reply = await connection.QueryFirstOrDefaultAsync<ForumReply>(
                    "SELECT * FROM ForumReplies WHERE ReplyId = @ReplyId",
                    new { ReplyId = id });

                if (reply == null)
                {
                    return Json(new { success = false, message = "Comment not found." });
                }

                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (!int.TryParse(userIdStr, out int userId) || (reply.UserId != userId && role != "Admin"))
                {
                    return Json(new { success = false, message = "Unauthorized to delete this comment." });
                }

                await _forumRepo.DeleteReplyAsync(id);
                return Json(new { success = true, message = "Comment deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting comment: " + ex.Message });
            }
        }
    }
}
