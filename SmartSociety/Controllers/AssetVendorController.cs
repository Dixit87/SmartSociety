using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize(Roles = "Admin,Technician")]
    public class AssetVendorController : Controller
    {
        private readonly IAssetVendorRepository _repository;
        private readonly IWebHostEnvironment _env;

        public AssetVendorController(IAssetVendorRepository repository, IWebHostEnvironment env)
        {
            _repository = repository;
            _env = env;
        }

        public async Task<IActionResult> Index(string? activeTab = "assets")
        {
            // Auto process due maintenance schedules
            try
            {
                await _repository.ProcessDueMaintenanceSchedulesAsync();
            }
            catch (Exception)
            {
                // Soft fail on page load if database is locked or has issue
            }

            var assets = await _repository.GetAllAssetsAsync();
            var vendors = await _repository.GetAllVendorsAsync();
            var schedules = await _repository.GetMaintenanceSchedulesAsync();
            var inventory = await _repository.GetAllInventoryItemsAsync();

            ViewBag.ActiveTab = activeTab;
            
            // Stats
            ViewBag.TotalAssets = assets.Count();
            ViewBag.TotalVendors = vendors.Count();
            
            var thirtyDaysFromNow = DateTime.Now.AddDays(30);
            ViewBag.ExpiringAMCs = assets.Count(a => a.AmcExpiryDate.HasValue && a.AmcExpiryDate.Value <= thirtyDaysFromNow && a.AmcExpiryDate.Value >= DateTime.Now);

            // Additional stats for Maintenance & Inventory
            ViewBag.TotalSchedules = schedules.Count();
            ViewBag.ActiveSchedules = schedules.Count(s => s.IsActive);
            ViewBag.TotalInventoryItems = inventory.Count();
            ViewBag.LowStockItems = inventory.Count(i => i.Quantity <= i.MinStockLevel);

            // Pass everything needed to ViewBag
            ViewBag.VendorList = vendors;
            ViewBag.AssetList = assets;
            ViewBag.Schedules = schedules;
            ViewBag.Inventory = inventory;

            // Use a Tuple to pass both lists to the view
            var model = new Tuple<IEnumerable<Asset>, IEnumerable<Vendor>>(assets, vendors);
            return View(model);
        }

        // --- VENDOR ENDPOINTS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveVendor(Vendor vendor, IFormFile? contractFile, string? removeContract)
        {
            try
            {
                if (removeContract == "true")
                {
                    vendor.ContractDocumentPath = "CLEAR";
                }
                else if (contractFile != null && contractFile.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(contractFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only PDF and image files (.pdf, .jpg, .jpeg, .png) are allowed for contract document." });
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "vendors");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(contractFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await contractFile.CopyToAsync(fileStream);
                    }
                    vendor.ContractDocumentPath = "/uploads/vendors/" + uniqueFileName;
                }

                await _repository.UpsertVendorAsync(vendor);
                return Json(new { success = true, message = "Vendor saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving the vendor." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVendor(int vendorId)
        {
            try
            {
                await _repository.DeleteVendorAsync(vendorId);
                return Json(new { success = true, message = "Vendor deleted successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting vendor." });
            }
        }

        // --- ASSET ENDPOINTS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveAsset(Asset asset, IFormFile? invoiceFile, string? removeInvoice)
        {
            try
            {
                if (removeInvoice == "true")
                {
                    asset.InvoiceDocumentPath = "CLEAR";
                }
                else if (invoiceFile != null && invoiceFile.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(invoiceFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only PDF and image files (.pdf, .jpg, .jpeg, .png) are allowed for invoice document." });
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "assets");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(invoiceFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await invoiceFile.CopyToAsync(fileStream);
                    }
                    asset.InvoiceDocumentPath = "/uploads/assets/" + uniqueFileName;
                }

                await _repository.UpsertAssetAsync(asset);
                return Json(new { success = true, message = "Asset saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while saving the asset." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsset(int assetId)
        {
            try
            {
                await _repository.DeleteAssetAsync(assetId);
                return Json(new { success = true, message = "Asset deleted successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting asset." });
            }
        }

        // --- SERVICE LOG ENDPOINTS ---
        [HttpGet]
        public async Task<IActionResult> GetServiceLogs(int assetId)
        {
            try
            {
                var logs = await _repository.GetServiceLogsByAssetIdAsync(assetId);
                return Json(new { success = true, data = logs });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error fetching service logs." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveServiceLog(AssetServiceLog log)
        {
            try
            {
                await _repository.UpsertServiceLogAsync(log);
                return Json(new { success = true, message = "Service log saved successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error saving service log." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteServiceLog(int logId)
        {
            try
            {
                await _repository.DeleteServiceLogAsync(logId);
                return Json(new { success = true, message = "Service log deleted!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting service log." });
            }
        }

        // --- MAINTENANCE SCHEDULE ENDPOINTS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveMaintenanceSchedule(MaintenanceSchedule schedule)
        {
            try
            {
                await _repository.UpsertMaintenanceScheduleAsync(schedule);
                return Json(new { success = true, message = "Maintenance schedule saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving maintenance schedule: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMaintenanceSchedule(int scheduleId)
        {
            try
            {
                await _repository.DeleteMaintenanceScheduleAsync(scheduleId);
                return Json(new { success = true, message = "Maintenance schedule deleted successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting maintenance schedule." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDueSchedules()
        {
            try
            {
                int processedCount = await _repository.ProcessDueMaintenanceSchedulesAsync();
                return Json(new { success = true, message = $"{processedCount} due schedule(s) processed and advanced successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing due schedules: " + ex.Message });
            }
        }

        // --- INVENTORY ENDPOINTS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveInventoryItem(InventoryItem item)
        {
            try
            {
                await _repository.UpsertInventoryItemAsync(item);
                return Json(new { success = true, message = "Inventory item saved successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving inventory item: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInventoryItem(int itemId)
        {
            try
            {
                await _repository.DeleteInventoryItemAsync(itemId);
                return Json(new { success = true, message = "Inventory item deleted successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error deleting inventory item." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestockInventoryItem(int itemId, int quantityToAdd)
        {
            try
            {
                var item = await _repository.GetInventoryItemByIdAsync(itemId);
                if (item == null)
                {
                    return Json(new { success = false, message = "Inventory item not found." });
                }

                item.Quantity += quantityToAdd;
                await _repository.UpsertInventoryItemAsync(item);
                return Json(new { success = true, message = "Stock replenished successfully!" });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error restocking item." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInventoryItemsJson()
        {
            try
            {
                var items = await _repository.GetAllInventoryItemsAsync();
                return Json(new { success = true, data = items });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error fetching inventory items." });
            }
        }
    }
}
