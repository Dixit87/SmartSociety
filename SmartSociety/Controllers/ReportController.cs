using Microsoft.AspNetCore.Mvc;
using SmartSociety.Repositories;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    public class ReportController : Controller
    {
        private readonly IReportRepository _repository;

        public ReportController(IReportRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var analytics = await _repository.GetDashboardAnalyticsAsync();
            return View(analytics);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData()
        {
            var analytics = await _repository.GetDashboardAnalyticsAsync();
            return Json(new { success = true, data = analytics });
        }
    }
}
