using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class IoTController : Controller
    {
        private readonly IIoTRepository _iotRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly IParkingRepository _parkingRepo;
        private readonly INotificationRepository _notificationRepo;

        public IoTController(
            IIoTRepository iotRepo,
            IFlatRepository flatRepo,
            IParkingRepository parkingRepo,
            INotificationRepository notificationRepo)
        {
            _iotRepo = iotRepo;
            _flatRepo = flatRepo;
            _parkingRepo = parkingRepo;
            _notificationRepo = notificationRepo;
        }

        // --- RFID Gate Console (Admin/Guard) ---

        [Authorize(Roles = "Admin,Guard")]
        [HttpGet]
        public async Task<IActionResult> GuardRfidConsole()
        {
            var slots = await _parkingRepo.GetAllParkingSlotsAsync();
            // Get slots with registered sticker numbers
            var registeredVehicles = slots.Where(s => !string.IsNullOrEmpty(s.StickerNumber) && s.FlatId.HasValue).ToList();
            ViewBag.RegisteredVehicles = registeredVehicles;

            var recentLogs = await _iotRepo.GetAllRfidGateLogsAsync();
            return View(recentLogs);
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulateRfidSweep(string rfidTag, string direction, string gateName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rfidTag))
                {
                    return Json(new { success = false, message = "RFID tag cannot be empty." });
                }

                // Check authorization
                var slots = await _parkingRepo.GetAllParkingSlotsAsync();
                var matchingSlot = slots.FirstOrDefault(s => string.Equals(s.StickerNumber, rfidTag, StringComparison.OrdinalIgnoreCase) && s.IsActive);
                
                string status = "Unauthorized - Denied";
                bool openGate = false;

                if (matchingSlot != null)
                {
                    status = "Authorized - Gate Opened";
                    openGate = true;
                }

                // Insert into RFID Log
                var log = await _iotRepo.InsertRfidGateLogAsync(rfidTag, direction, gateName, status);

                return Json(new { 
                    success = true, 
                    openGate = openGate, 
                    log = log, 
                    message = openGate ? "Access Granted. Opening Gate..." : "Access Denied. RFID Tag not recognized." 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpGet]
        public async Task<IActionResult> GetRecentGateLogs()
        {
            try
            {
                var logs = await _iotRepo.GetAllRfidGateLogsAsync();
                return Json(new { success = true, logs = logs.Take(15) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // --- Admin Smart Meters (Admin) ---

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminSmartMeters()
        {
            var meters = await _iotRepo.GetAllSmartMetersAsync();
            var flats = await _flatRepo.GetAllFlatsAsync();
            
            ViewBag.Flats = flats;
            return View(meters);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrUpdateMeter(SmartMeter meter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(meter.MeterNumber) || meter.FlatId <= 0)
                {
                    return Json(new { success = false, message = "Meter Number and Flat are required." });
                }

                // Check unique meter number
                var allMeters = await _iotRepo.GetAllSmartMetersAsync();
                var exists = allMeters.Any(m => string.Equals(m.MeterNumber, meter.MeterNumber, StringComparison.OrdinalIgnoreCase) && m.MeterId != meter.MeterId);
                if (exists)
                {
                    return Json(new { success = false, message = "A meter with this number already exists." });
                }

                await _iotRepo.SaveSmartMeterAsync(meter);
                return Json(new { success = true, message = "Smart Meter configuration saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulateConsumption()
        {
            try
            {
                var meters = await _iotRepo.GetAllSmartMetersAsync();
                var random = new Random();
                int alertCount = 0;

                foreach (var meter in meters)
                {
                    decimal units = 0;
                    decimal rate = meter.RatePerUnit > 0 ? meter.RatePerUnit : 5.50m;
                    
                    if (string.Equals(meter.MeterType, "Electricity", StringComparison.OrdinalIgnoreCase))
                    {
                        // Electricity: 2.0 to 10.0 units
                        units = (decimal)(random.NextDouble() * 8.0 + 2.0);
                    }
                    else
                    {
                        // Water: 20.0 to 80.0 Liters
                        units = (decimal)(random.NextDouble() * 60.0 + 20.0);
                    }

                    decimal cost = units * rate;
                    var result = await _iotRepo.ConsumeMeterBalanceAsync(meter.MeterId, units, cost);

                    // Inspect the resulting balance and status dynamically
                    decimal newBalance = (decimal)result.Balance;
                    string status = (string)result.Status;

                    // Trigger notification in system if balance is low or suspended
                    var flat = await _flatRepo.GetFlatByIdAsync(meter.FlatId);
                    if (flat != null && flat.OwnerId.HasValue)
                    {
                        int ownerUserId = flat.OwnerId.Value;

                        // Balance fell below threshold (₹150)
                        if (newBalance < 150.00m && newBalance > 0 && meter.Balance >= 150.00m)
                        {
                            await _notificationRepo.InsertAsync(new Notification
                            {
                                UserId = ownerUserId,
                                Title = "Low Smart Meter Balance Alert",
                                Message = $"Your prepaid meter {meter.MeterNumber} ({meter.MeterType}) balance is low: ₹{newBalance:0.00}. Please recharge soon to avoid suspension.",
                                Category = "SmartMeter",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            });
                            alertCount++;
                        }
                        // Meter got suspended
                        else if (newBalance <= 0 && string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase) && !string.Equals(meter.Status, "Suspended", StringComparison.OrdinalIgnoreCase))
                        {
                            await _notificationRepo.InsertAsync(new Notification
                            {
                                UserId = ownerUserId,
                                Title = "Smart Meter Service Suspended",
                                Message = $"Your prepaid meter {meter.MeterNumber} ({meter.MeterType}) has been suspended due to insufficient balance: ₹{newBalance:0.00}. Please recharge immediately to resume service.",
                                Category = "SmartMeter",
                                IsRead = false,
                                CreatedAt = DateTime.Now
                            });
                            alertCount++;
                        }
                    }
                }

                return Json(new { success = true, message = $"Consumption simulated successfully for all active meters. Sent {alertCount} low balance/suspension notifications." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // --- Resident Prepaid Smart Meters ---

        [Authorize(Roles = "Resident")]
        [HttpGet]
        public async Task<IActionResult> MySmartMeters()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    var meters = await _iotRepo.GetSmartMetersByFlatIdAsync(flat.FlatId);
                    ViewBag.Flat = flat;
                    return View(meters);
                }
            }

            return View(new List<SmartMeter>());
        }

        [Authorize(Roles = "Resident")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechargeMeter(int meterId, decimal amount, string paymentMethod)
        {
            try
            {
                if (amount <= 0)
                {
                    return Json(new { success = false, message = "Recharge amount must be greater than zero." });
                }

                var meter = await _iotRepo.GetSmartMeterByIdAsync(meterId);
                if (meter == null)
                {
                    return Json(new { success = false, message = "Smart Meter not found." });
                }

                string txnId = "TXN-IOT-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
                var result = await _iotRepo.RechargeMeterAsync(meterId, amount, paymentMethod, txnId);

                // Add a notification about successful recharge
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    await _notificationRepo.InsertAsync(new Notification
                    {
                        UserId = userId,
                        Title = "Meter Recharge Successful",
                        Message = $"Recharge of ₹{amount:0.00} for meter {meter.MeterNumber} ({meter.MeterType}) was successful. New balance: ₹{(decimal)result.Balance:0.00}.",
                        Category = "SmartMeter",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }

                return Json(new { 
                    success = true, 
                    newBalance = (decimal)result.Balance, 
                    txnId = txnId, 
                    message = $"Meter recharged successfully with ₹{amount:0.00}. Transaction ID: {txnId}" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [Authorize(Roles = "Resident")]
        [HttpGet]
        public async Task<IActionResult> GetMeterLogs(int meterId)
        {
            try
            {
                var logs = await _iotRepo.GetMeterLogsAsync(meterId);
                return Json(new { success = true, logs = logs.Take(10) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [Authorize(Roles = "Resident")]
        [HttpGet]
        public async Task<IActionResult> GetMeterRecharges(int meterId)
        {
            try
            {
                var recharges = await _iotRepo.GetMeterRechargesAsync(meterId);
                return Json(new { success = true, recharges = recharges.Take(10) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // --- Resident Parking & RFID Logs ---

        [Authorize(Roles = "Resident")]
        [HttpGet]
        public async Task<IActionResult> MyParkingLogs()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    // Get parking slots allocated to this flat
                    var slots = await _parkingRepo.GetAllParkingSlotsAsync();
                    var mySlots = slots.Where(s => s.FlatId == flat.FlatId).ToList();

                    // Get RFID entry logs for this flat
                    var logs = await _iotRepo.GetRfidGateLogsByFlatIdAsync(flat.FlatId);

                    ViewBag.MySlots = mySlots;
                    ViewBag.Flat = flat;

                    return View(logs);
                }
            }

            ViewBag.MySlots = new List<ParkingSlot>();
            return View(new List<RfidGateLog>());
        }
    }
}
