using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class PollController : Controller
    {
        private readonly IPollRepository _repository;

        public PollController(IPollRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var polls = await _repository.GetAllPollsAsync();
            return View(polls);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePoll([FromBody] PollUpsertViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Question) || model.Options == null || model.Options.Count < 2)
                {
                    return Json(new { success = false, message = "A question and at least two options are required." });
                }

                await _repository.CreatePollAsync(model);
                return Json(new { success = true, message = "Poll created successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error creating poll. " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePoll(int pollId)
        {
            try
            {
                await _repository.DeletePollAsync(pollId);
                return Json(new { success = true, message = "Poll deleted successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting poll." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPollResults(int pollId)
        {
            try
            {
                var poll = await _repository.GetPollByIdAsync(pollId);
                if (poll == null) return Json(new { success = false, message = "Poll not found." });

                return Json(new { success = true, data = poll });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error fetching results." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CastVote(int pollId, int optionId)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Json(new { success = false, message = "Unauthorized. Please login to cast your vote." });
                }

                bool result = await _repository.CastVoteAsync(pollId, optionId, userId);
                if (!result)
                {
                    return Json(new { success = false, message = "You have already voted in this poll!" });
                }

                return Json(new { success = true, message = "Your vote has been cast successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error casting vote: " + ex.Message });
            }
        }
    }
}
