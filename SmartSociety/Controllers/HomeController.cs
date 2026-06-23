using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;

        public HomeController(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<IActionResult> Index()
        {
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "Guest";

            if (role == "Admin")
            {
                var adminData = await _dashboardRepository.GetAdminSummaryAsync();
                return View("AdminDashboard", adminData);
            }
            else if (role == "Resident")
            {
                return View("ResidentDashboard");
            }
            else if (role == "Guard")
            {
                return View("GuardDashboard");
            }

            return View(); // Fallback
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
