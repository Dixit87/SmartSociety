using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    [Authorize(Roles = "Admin,Accountant,Resident")]
    public class MaintenanceController : Controller
    {
        private readonly IMaintenanceRepository _maintenanceRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly INotificationRepository _notificationRepo;

        public MaintenanceController(
            IMaintenanceRepository maintenanceRepo, 
            IFlatRepository flatRepo,
            INotificationRepository notificationRepo)
        {
            _maintenanceRepo = maintenanceRepo;
            _flatRepo = flatRepo;
            _notificationRepo = notificationRepo;
        }

        [Authorize(Roles = "Admin,Accountant")]
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

                // Notify residents of active flats
                var flats = await _flatRepo.GetAllFlatsAsync();
                foreach (var flat in flats)
                {
                    int? targetUserId = flat.TenantId ?? flat.OwnerId;
                    if (targetUserId.HasValue)
                    {
                        await _notificationRepo.InsertAsync(new Notification
                        {
                            UserId = targetUserId.Value,
                            Title = "New Maintenance Bill Generated",
                            Message = $"Your maintenance bill for {month}/{year} has been generated. Please view details and complete the payment.",
                            Category = "Maintenance"
                        });
                    }
                }

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

                // Notify resident
                var receipt = await _maintenanceRepo.GetBillReceiptAsync(billId);
                if (receipt != null && receipt.Bill != null)
                {
                    var flat = await _flatRepo.GetFlatByIdAsync(receipt.Bill.FlatId);
                    if (flat != null)
                    {
                        int? targetUserId = flat.TenantId ?? flat.OwnerId;
                        if (targetUserId.HasValue)
                        {
                            await _notificationRepo.InsertAsync(new Notification
                            {
                                UserId = targetUserId.Value,
                                Title = "Payment Recorded Successfully",
                                Message = $"A payment of ₹{paidAmount} was successfully recorded for your flat {flat.BlockName}-{flat.FlatNumber} for the maintenance bill of {receipt.Bill.DisplayMonthYear}.",
                                Category = "Maintenance"
                            });
                        }
                    }
                }

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
            var settings = await _maintenanceRepo.GetSettingsAsync();
            ViewBag.Settings = settings;
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
        [Authorize(Roles = "Admin,Accountant")]
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

        [HttpGet]
        public async Task<IActionResult> MyBills()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    var bills = await _maintenanceRepo.GetBillsByFlatIdAsync(flat.FlatId);
                    ViewBag.Flat = flat;
                    return View(bills);
                }
            }

            return View(new List<MaintenanceBill>());
        }
    }
}
