using Microsoft.AspNetCore.Mvc;
using SmartSociety.Repositories;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogController : Controller
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogController(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _repository.GetAllLogsAsync();
            return View(logs);
        }
    }
}
