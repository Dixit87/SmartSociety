using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartSociety.Models;
using SmartSociety.Repositories;

namespace SmartSociety.Controllers
{
    public class VisitorController : Controller
    {
        private readonly IVisitorRepository _visitorRepo;
        private readonly IFlatRepository _flatRepo;

        public VisitorController(IVisitorRepository visitorRepo, IFlatRepository flatRepo)
        {
            _visitorRepo = visitorRepo;
            _flatRepo = flatRepo;
        }

        public async Task<IActionResult> Index()
        {
            var visitors = await _visitorRepo.GetTodayVisitorsAsync();
            
            // Dashboard Stats
            ViewBag.TotalToday = visitors.Count();
            ViewBag.Inside = visitors.Count(v => v.Status == "Inside");
            ViewBag.Exited = visitors.Count(v => v.Status == "Exited" || v.Status == "Rejected");
            ViewBag.Pending = visitors.Count(v => v.Status == "Pending");

            return View(visitors);
        }

        [HttpGet]
        public async Task<IActionResult> History(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                startDate = DateTime.Today.AddDays(-7);
                endDate = DateTime.Today;
            }

            var visitors = await _visitorRepo.GetVisitorHistoryAsync(startDate.Value, endDate.Value);
            
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View("Index", visitors); // Reuse the Index view but with history data
        }

        [HttpGet]
        public async Task<IActionResult> GetFlatsDropdown()
        {
            var flats = await _flatRepo.GetAllFlatsAsync();
            var list = flats.Select(f => new SelectListItem {
                Text = $"{f.BlockName} - {f.FlatNumber} ({f.OwnerName ?? f.TenantName ?? "No Resident"})",
                Value = f.FlatId.ToString()
            }).ToList();
            
            return Json(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entry(Visitor visitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _visitorRepo.EntryVisitorAsync(visitor);
                    return Json(new { success = true, message = "Visitor entry logged successfully." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Invalid data provided." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id)
        {
            try
            {
                await _visitorRepo.CheckoutVisitorAsync(id);
                return Json(new { success = true, message = "Visitor checked out successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error during checkout: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                await _visitorRepo.ApproveVisitorAsync(id);
                return Json(new { success = true, message = "Entry approved. Visitor is now inside." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error approving visitor: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                await _visitorRepo.RejectVisitorAsync(id);
                return Json(new { success = true, message = "Entry rejected. Visitor marked as exited/rejected." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error rejecting visitor: " + ex.Message });
            }
        }
    }
}
