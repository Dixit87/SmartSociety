using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    public class CommunicationController : Controller
    {
        private readonly ICommunicationRepository _repository;

        public CommunicationController(ICommunicationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _repository.GetAllMessageLogsAsync();
            return View(logs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(MessageLog log)
        {
            try
            {
                // In a real application, you would connect to an SMS/Email Gateway here.
                // For example: await _emailService.SendEmailAsync(log.Audience, log.Subject, log.Body);
                
                // Simulate network delay for Mock Sending
                await Task.Delay(1500);

                log.Status = "Sent";
                await _repository.LogMessageAsync(log);

                return Json(new { success = true, message = $"{log.MessageType} sent successfully to {log.Audience}!" });
            }
            catch (Exception ex)
            {
                // Log failed attempts as well if needed
                log.Status = "Failed";
                await _repository.LogMessageAsync(log);
                return Json(new { success = false, message = "Error sending message. " + ex.Message });
            }
        }
    }
}
