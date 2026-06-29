using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IFlatRepository _flatRepo;
        private readonly IVisitorRepository _visitorRepo;
        private readonly IComplaintRepository _complaintRepo;
        private readonly IAssetVendorRepository _assetRepo;

        public HomeController(
            IDashboardRepository dashboardRepository,
            IFlatRepository flatRepo,
            IVisitorRepository visitorRepo,
            IComplaintRepository complaintRepo,
            IAssetVendorRepository assetRepo)
        {
            _dashboardRepository = dashboardRepository;
            _flatRepo = flatRepo;
            _visitorRepo = visitorRepo;
            _complaintRepo = complaintRepo;
            _assetRepo = assetRepo;
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
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                    if (flat != null)
                    {
                        var residentData = await _dashboardRepository.GetResidentSummaryAsync(userId, flat.FlatId);
                        return View("ResidentDashboard", residentData);
                    }
                }
                
                // Return empty view model if user details are not resolved
                return View("ResidentDashboard", new ResidentDashboardViewModel());
            }
            else if (role == "Guard")
            {
                var todayVisitors = await _visitorRepo.GetTodayVisitorsAsync();
                var viewModel = new GuardDashboardViewModel
                {
                    TodayTotalVisitorsCount = todayVisitors.Count(),
                    ActiveVisitorsCount = todayVisitors.Count(v => v.Status == "Inside"),
                    PendingApprovalsCount = todayVisitors.Count(v => v.Status == "Pending"),
                    ActiveVisitors = todayVisitors.Where(v => v.Status == "Inside").OrderByDescending(v => v.InTime).ToList(),
                    ExpectedVisitors = todayVisitors.Where(v => v.Status == "Pending").OrderBy(v => v.InTime).ToList()
                };
                return View("GuardDashboard", viewModel);
            }
            else if (role == "Accountant")
            {
                return RedirectToAction("Index", "Finance");
            }
            else if (role == "Technician")
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var complaints = await _complaintRepo.GetAllAsync();
                    var techComplaints = complaints.Where(c => c.AssignedTo == userId).ToList();

                    var assets = await _assetRepo.GetAllAssetsAsync();
                    var targetDate = System.DateTime.Now.AddDays(30);
                    var expiringAssets = assets.Where(a => a.AmcExpiryDate.HasValue && a.AmcExpiryDate.Value <= targetDate && a.AmcExpiryDate.Value >= System.DateTime.Now).ToList();

                    var viewModel = new TechnicianDashboardViewModel
                    {
                        AssignedComplaintsCount = techComplaints.Count(c => c.Status == "Open"),
                        InProgressComplaintsCount = techComplaints.Count(c => c.Status == "InProgress"),
                        ResolvedComplaintsCount = techComplaints.Count(c => c.Status == "Resolved" || c.Status == "Closed"),
                        ActiveComplaints = techComplaints.Where(c => c.Status == "Open" || c.Status == "InProgress").OrderByDescending(c => c.CreatedAt).ToList(),
                        ResolvedHistory = techComplaints.Where(c => c.Status == "Resolved" || c.Status == "Closed").OrderByDescending(c => c.UpdatedAt).ToList(),
                        AssetsRequiringService = expiringAssets
                    };

                    return View("TechnicianDashboard", viewModel);
                }

                return View("TechnicianDashboard", new TechnicianDashboardViewModel());
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
