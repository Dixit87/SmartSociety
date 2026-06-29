using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.Dynamic;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize(Roles = "Admin,Accountant,Resident")]
    public class UtilityController : Controller
    {
        private readonly IUtilityRepository _utilityRepo;
        private readonly IFlatRepository _flatRepo;

        public UtilityController(IUtilityRepository utilityRepo, IFlatRepository flatRepo)
        {
            _utilityRepo = utilityRepo;
            _flatRepo = flatRepo;
        }

        [Authorize(Roles = "Admin,Accountant")]
        public async Task<IActionResult> Index(int? month, int? year)
        {
            ViewBag.CurrentMonth = month ?? DateTime.Now.Month;
            ViewBag.CurrentYear = year ?? DateTime.Now.Year;

            var stats = await _utilityRepo.GetDashboardStatsAsync();
            var bills = await _utilityRepo.GetUtilityBillsAsync(ViewBag.CurrentMonth, ViewBag.CurrentYear);
            var utilityTypes = await _utilityRepo.GetUtilityTypesAsync();
            var flats = await _flatRepo.GetAllFlatsAsync();

            dynamic model = new ExpandoObject();
            model.Stats = stats;
            model.Bills = bills;
            model.UtilityTypes = utilityTypes;
            model.Flats = flats;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var types = await _utilityRepo.GetUtilityTypesAsync();
            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUtilityType(UtilityType type)
        {
            try
            {
                await _utilityRepo.SaveUtilityTypeAsync(type);
                TempData["SuccessMessage"] = "Utility Type saved successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            return RedirectToAction(nameof(Settings));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUtilityType(int id)
        {
            try
            {
                await _utilityRepo.DeleteUtilityTypeAsync(id);
                TempData["SuccessMessage"] = "Utility Type deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
            }
            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> GetPreviousReading(int flatId, int utilityTypeId)
        {
            try
            {
                var prevReading = await _utilityRepo.GetPreviousReadingAsync(flatId, utilityTypeId);
                return Json(new { success = true, previousReading = prevReading });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordReading(int flatId, int utilityTypeId, int month, int year, decimal currentReading, decimal? overridePreviousReading)
        {
            try
            {
                await _utilityRepo.RecordReadingAsync(flatId, utilityTypeId, month, year, currentReading, overridePreviousReading);
                return Json(new { success = true, message = "Meter reading recorded and bill generated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBill(int billId, decimal previousReading, decimal currentReading)
        {
            try
            {
                await _utilityRepo.UpdateBillAsync(billId, previousReading, currentReading);
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
                await _utilityRepo.DeleteBillAsync(billId);
                return Json(new { success = true, message = "Bill deleted successfully." });
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
                await _utilityRepo.RecordPaymentAsync(billId, paidAmount, paymentMode, transactionId, remarks);
                return Json(new { success = true, message = "Utility payment recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var receiptModel = await _utilityRepo.GetReceiptAsync(id);
            if (receiptModel.Bill.BillId == 0)
            {
                return NotFound();
            }
            return View(receiptModel);
        }

        [HttpGet]
        public async Task<IActionResult> MyUsage()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    var bills = await _utilityRepo.GetUtilityBillsByFlatIdAsync(flat.FlatId);
                    ViewBag.Flat = flat;
                    return View(bills);
                }
            }

            return View(new List<UtilityBill>());
        }
    }
}
