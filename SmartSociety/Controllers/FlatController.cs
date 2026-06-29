using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using SmartSociety.Models;
using SmartSociety.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FlatController : Controller
    {
        private readonly IFlatRepository _flatRepo;
        private readonly IBlockRepository _blockRepo;
        private readonly IUserRepository _userRepo;

        public FlatController(IFlatRepository flatRepo, IBlockRepository blockRepo, IUserRepository userRepo)
        {
            _flatRepo = flatRepo;
            _blockRepo = blockRepo;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            var flats = await _flatRepo.GetAllFlatsAsync();
            var blocks = await _blockRepo.GetAllBlocksAsync();
            ViewBag.Blocks = blocks;
            
            // Dashboard Stats
            ViewBag.TotalFlats = flats.Count();
            ViewBag.VacantFlats = flats.Count(f => string.IsNullOrEmpty(f.OwnerName) && string.IsNullOrEmpty(f.TenantName));
            ViewBag.SelfOccupiedFlats = flats.Count(f => !string.IsNullOrEmpty(f.OwnerName) && string.IsNullOrEmpty(f.TenantName));
            ViewBag.RentedFlats = flats.Count(f => !string.IsNullOrEmpty(f.TenantName));
            
            return View(flats);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            FlatUpsertViewModel vm = new FlatUpsertViewModel();
            
            // Populate Dropdowns
            var blocks = await _blockRepo.GetAllBlocksAsync();
            vm.BlockList = blocks.Select(b => new SelectListItem {
                Text = b.BlockName,
                Value = b.BlockId.ToString()
            });

            var users = await _userRepo.GetAllUsersAsync();
            var residentsAndOwners = users.Where(u => u.Role == "Resident" || u.Role == "Admin");
            vm.UserList = residentsAndOwners.Select(u => new SelectListItem {
                Text = $"{u.FullName} (ID: {u.UserId})",
                Value = u.UserId.ToString()
            });

            if (id.HasValue && id.Value > 0)
            {
                var flat = await _flatRepo.GetFlatByIdAsync(id.Value);
                if (flat == null)
                {
                    return NotFound();
                }
                vm.Flat = flat;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(FlatUpsertViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Prevent duplicate block + flat number
                    var flats = await _flatRepo.GetAllFlatsAsync();
                    var duplicateFlat = flats.FirstOrDefault(f => 
                        f.BlockId == vm.Flat.BlockId && 
                        string.Equals(f.FlatNumber?.Trim(), vm.Flat.FlatNumber?.Trim(), StringComparison.OrdinalIgnoreCase) && 
                        f.FlatId != vm.Flat.FlatId);

                    if (duplicateFlat != null)
                    {
                        ModelState.AddModelError("Flat.FlatNumber", $"Flat number {vm.Flat.FlatNumber} already exists in this block.");
                    }
                    else
                    {
                        await _flatRepo.UpsertFlatAsync(vm.Flat);
                        TempData["SuccessMessage"] = vm.Flat.FlatId == 0 ? "Flat created successfully." : "Flat updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            // Repopulate dropdowns if validation fails
            var blocks = await _blockRepo.GetAllBlocksAsync();
            vm.BlockList = blocks.Select(b => new SelectListItem {
                Text = b.BlockName,
                Value = b.BlockId.ToString()
            });

            var users = await _userRepo.GetAllUsersAsync();
            var residentsAndOwners = users.Where(u => u.Role == "Resident" || u.Role == "Admin");
            vm.UserList = residentsAndOwners.Select(u => new SelectListItem {
                Text = $"{u.FullName} (ID: {u.UserId})",
                Value = u.UserId.ToString()
            });

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _flatRepo.DeleteFlatAsync(id);
                return Json(new { success = true, message = "Flat deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting flat: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var flat = await _flatRepo.GetFlatByIdAsync(id);
                if (flat == null)
                {
                    return Json(new { success = false, message = "Flat not found." });
                }

                flat.IsActive = !flat.IsActive;
                await _flatRepo.UpsertFlatAsync(flat);

                return Json(new { success = true, message = $"Flat status updated to {(flat.IsActive ? "Active" : "Inactive")}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating flat status: " + ex.Message });
            }
        }

        // --- Block Management APIs ---

        [HttpGet]
        public async Task<IActionResult> GetBlocks()
        {
            var blocks = await _blockRepo.GetAllBlocksAsync();
            return Json(new { data = blocks });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertBlock(Block block)
        {
            try
            {
                if (string.IsNullOrEmpty(block.BlockName))
                {
                    return Json(new { success = false, message = "Block Name is required." });
                }
                
                await _blockRepo.UpsertBlockAsync(block);
                return Json(new { success = true, message = block.BlockId == 0 ? "Block added successfully." : "Block updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving block: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBlock(int id)
        {
            try
            {
                // Check if any flats are using this block
                var flats = await _flatRepo.GetAllFlatsAsync();
                if (flats.Any(f => f.BlockId == id))
                {
                    return Json(new { success = false, message = "Cannot delete this block because there are flats associated with it." });
                }

                await _blockRepo.DeleteBlockAsync(id);
                return Json(new { success = true, message = "Block deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting block: " + ex.Message });
            }
        }

        // --- Bulk Upload APIs ---

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Flats");
                
                worksheet.Cell(1, 1).Value = "BlockName";
                worksheet.Cell(1, 2).Value = "FlatNumber";
                worksheet.Cell(1, 3).Value = "FloorNumber";
                worksheet.Cell(1, 4).Value = "FlatType";
                worksheet.Cell(1, 5).Value = "AreaSqFt";
                worksheet.Cell(1, 6).Value = "IntercomNumber";
                
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Flat_Bulk_Template.xlsx");
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(IFormFile uploadFile)
        {
            if (uploadFile == null || uploadFile.Length == 0)
            {
                return Json(new { success = false, message = "Please select a valid Excel file." });
            }

            if (!uploadFile.FileName.EndsWith(".xlsx"))
            {
                return Json(new { success = false, message = "Only .xlsx files are supported." });
            }

            int successCount = 0;
            int errorCount = 0;

            try
            {
                var blocks = await _blockRepo.GetAllBlocksAsync();
                var existingFlats = await _flatRepo.GetAllFlatsAsync();
                var uploadedFlats = new List<(string Block, string Flat)>();

                using (var stream = new MemoryStream())
                {
                    await uploadFile.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rangeUsed = worksheet.RangeUsed();
                        if (rangeUsed != null)
                        {
                            var rows = rangeUsed.RowsUsed().Skip(1);

                            foreach (var row in rows)
                            {
                                try
                                {
                                    string blockName = row.Cell(1).GetString().Trim();
                                    string flatNo = row.Cell(2).GetString().Trim();
                                    string floorStr = row.Cell(3).GetString().Trim();
                                    string type = row.Cell(4).GetString().Trim();
                                    string areaStr = row.Cell(5).GetString().Trim();
                                    string intercom = row.Cell(6).GetString().Trim();

                                    if (string.IsNullOrEmpty(blockName) || string.IsNullOrEmpty(flatNo) || !int.TryParse(floorStr, out int floor) || !decimal.TryParse(areaStr, out decimal area))
                                    {
                                        errorCount++;
                                        continue;
                                    }

                                    // Auto-create block if it doesn't exist
                                    var block = blocks.FirstOrDefault(b => b.BlockName.Equals(blockName, StringComparison.OrdinalIgnoreCase));
                                    if (block == null)
                                    {
                                        block = new Block { BlockName = blockName, TotalFloors = floor > 10 ? floor : 10 };
                                        int blockId = await _blockRepo.UpsertBlockAsync(block);
                                        block.BlockId = blockId;
                                        // Refresh blocks collection
                                        blocks = await _blockRepo.GetAllBlocksAsync();
                                    }

                                    // Prevent duplicates in DB or Excel sheet upload
                                    var existsInDb = existingFlats.Any(f => 
                                        string.Equals(f.BlockName?.Trim(), blockName, StringComparison.OrdinalIgnoreCase) && 
                                        string.Equals(f.FlatNumber?.Trim(), flatNo, StringComparison.OrdinalIgnoreCase));

                                    var existsInExcel = uploadedFlats.Any(f => 
                                        string.Equals(f.Block, blockName, StringComparison.OrdinalIgnoreCase) && 
                                        string.Equals(f.Flat, flatNo, StringComparison.OrdinalIgnoreCase));

                                    if (existsInDb || existsInExcel)
                                    {
                                        errorCount++;
                                        continue;
                                    }

                                    uploadedFlats.Add((blockName, flatNo));

                                    var flat = new Flat
                                    {
                                        BlockId = block.BlockId,
                                        FlatNumber = flatNo,
                                        FloorNumber = floor,
                                        FlatType = type,
                                        AreaSqFt = area,
                                        IntercomNumber = intercom,
                                        IsActive = true
                                    };

                                    await _flatRepo.UpsertFlatAsync(flat);
                                    successCount++;
                                }
                                catch
                                {
                                    errorCount++;
                                }
                            }
                        }
                    }
                }

                string msg = $"Bulk upload completed. {successCount} flats imported successfully.";
                if (errorCount > 0)
                {
                    msg += $" {errorCount} rows failed or had duplicate/invalid entries.";
                }

                return Json(new { success = true, message = msg });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error processing file: " + ex.Message });
            }
        }
    }
}
