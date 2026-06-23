using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using System.IO;

namespace SmartSociety.Controllers
{
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
            var assets = await _repository.GetAllAssetsAsync();
            var vendors = await _repository.GetAllVendorsAsync();

            ViewBag.ActiveTab = activeTab;
            
            // Stats
            ViewBag.TotalAssets = assets.Count();
            ViewBag.TotalVendors = vendors.Count();
            
            var thirtyDaysFromNow = DateTime.Now.AddDays(30);
            ViewBag.ExpiringAMCs = assets.Count(a => a.AmcExpiryDate.HasValue && a.AmcExpiryDate.Value <= thirtyDaysFromNow && a.AmcExpiryDate.Value >= DateTime.Now);

            // Need to pass vendors to view for the Asset AMC Vendor dropdown
            ViewBag.VendorList = vendors;

            // Use a Tuple to pass both lists to the view
            var model = new Tuple<IEnumerable<Asset>, IEnumerable<Vendor>>(assets, vendors);
            return View(model);
        }

        // --- VENDOR ENDPOINTS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
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
    }
}
