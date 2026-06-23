using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
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
        public async Task<IActionResult> MockVote(int pollId, int optionId)
        {
            try
            {
                await _repository.MockVoteAsync(pollId, optionId);
                return Json(new { success = true, message = "Mock vote cast successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error casting mock vote." });
            }
        }
    }
}
