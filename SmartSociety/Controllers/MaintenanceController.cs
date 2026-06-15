using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;

namespace SmartSociety.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly IMaintenanceRepository _maintenanceRepo;

        public MaintenanceController(IMaintenanceRepository maintenanceRepo)
        {
            _maintenanceRepo = maintenanceRepo;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            if (!month.HasValue || !year.HasValue)
            {
                month = DateTime.Now.Month;
                year = DateTime.Now.Year;
            }

            ViewBag.CurrentMonth = month;
            ViewBag.CurrentYear = year;

            var stats = await _maintenanceRepo.GetDashboardStatsAsync();
            ViewBag.Stats = stats;

            var bills = await _maintenanceRepo.GetBillsAsync(month, year);
            return View(bills);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var settings = await _maintenanceRepo.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(MaintenanceSetting setting)
        {
            if (ModelState.IsValid)
            {
                await _maintenanceRepo.UpdateSettingsAsync(setting);
                TempData["SuccessMessage"] = "Maintenance settings updated successfully.";
                return RedirectToAction(nameof(Settings));
            }
            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBulkBills(int month, int year, decimal extraCharges, string? extraChargeRemarks)
        {
            try
            {
                await _maintenanceRepo.GenerateBulkBillsAsync(month, year, extraCharges, extraChargeRemarks);
                return Json(new { success = true, message = "Bills generated successfully for all active flats." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(int billId, decimal paidAmount, string paymentMode, string? transactionId, string? remarks)
        {
            try
            {
                await _maintenanceRepo.RecordPaymentAsync(billId, paidAmount, paymentMode, transactionId, remarks);
                return Json(new { success = true, message = "Payment recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var receipt = await _maintenanceRepo.GetBillReceiptAsync(id);
            if (receipt == null)
            {
                return NotFound("Receipt not found.");
            }
            return View(receipt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPenalties()
        {
            try
            {
                int count = await _maintenanceRepo.ApplyPenaltiesAsync();
                return Json(new { success = true, message = $"Penalties applied to {count} overdue bills." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBill(int billId, decimal baseAmount, decimal extraCharges, string? extraChargeRemarks)
        {
            try
            {
                await _maintenanceRepo.UpdateBillAsync(billId, baseAmount, extraCharges, extraChargeRemarks);
                return Json(new { success = true, message = "Bill updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBill(int billId)
        {
            try
            {
                await _maintenanceRepo.DeleteBillAsync(billId);
                return Json(new { success = true, message = "Bill deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }
    }
}
