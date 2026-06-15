using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using SmartSociety.Models;
using SmartSociety.Repositories;

namespace SmartSociety.Controllers
{
    public class ParkingController : Controller
    {
        private readonly IParkingRepository _parkingRepo;
        private readonly IFlatRepository _flatRepo;

        public ParkingController(IParkingRepository parkingRepo, IFlatRepository flatRepo)
        {
            _parkingRepo = parkingRepo;
            _flatRepo = flatRepo;
        }

        public async Task<IActionResult> Index()
        {
            var slots = await _parkingRepo.GetAllParkingSlotsAsync();
            
            // Dashboard Stats
            ViewBag.TotalSlots = slots.Count();
            ViewBag.AllocatedSlots = slots.Count(s => s.FlatId.HasValue);
            ViewBag.AvailableSlots = slots.Count(s => !s.FlatId.HasValue);

            return View(slots);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            var vm = new ParkingSlotUpsertViewModel();
            
            // Populate Flats Dropdown formatted nicely
            var flats = await _flatRepo.GetAllFlatsAsync();
            vm.FlatList = flats.Select(f => new SelectListItem {
                Text = $"{f.BlockName} - {f.FlatNumber} ({f.OwnerName ?? "No Owner"})",
                Value = f.FlatId.ToString()
            });

            if (id.HasValue && id.Value > 0)
            {
                var slot = await _parkingRepo.GetParkingSlotByIdAsync(id.Value);
                if (slot == null)
                {
                    return NotFound();
                }
                vm.ParkingSlot = slot;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ParkingSlotUpsertViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (vm.ParkingSlot.IsVisitorSlot)
                    {
                        vm.ParkingSlot.FlatId = null;
                        vm.ParkingSlot.VehicleNumber = null;
                        vm.ParkingSlot.VehicleMakeModel = null;
                        vm.ParkingSlot.StickerNumber = null;
                    }

                    await _parkingRepo.UpsertParkingSlotAsync(vm.ParkingSlot);
                    TempData["SuccessMessage"] = vm.ParkingSlot.SlotId == 0 ? "Parking slot created successfully." : "Parking slot updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred: " + ex.Message);
                }
            }

            var flats = await _flatRepo.GetAllFlatsAsync();
            vm.FlatList = flats.Select(f => new SelectListItem {
                Text = $"{f.BlockName} - {f.FlatNumber} ({f.OwnerName ?? "No Owner"})",
                Value = f.FlatId.ToString()
            });

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _parkingRepo.DeleteParkingSlotAsync(id);
                return Json(new { success = true, message = "Parking slot deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deleting parking slot: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var slot = await _parkingRepo.GetParkingSlotByIdAsync(id);
                if (slot == null)
                {
                    return Json(new { success = false, message = "Slot not found." });
                }

                slot.IsActive = !slot.IsActive;
                await _parkingRepo.UpsertParkingSlotAsync(slot);

                return Json(new { success = true, message = $"Slot status updated to {(slot.IsActive ? "Active" : "Inactive")}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating slot status: " + ex.Message });
            }
        }

        // --- Bulk Upload APIs ---

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("ParkingSlots");
                
                worksheet.Cell(1, 1).Value = "SlotNumber";
                worksheet.Cell(1, 2).Value = "VehicleType";
                worksheet.Cell(1, 3).Value = "IsVisitorSlot (Yes/No)";
                
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ParkingSlot_Bulk_Template.xlsx");
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
                using (var stream = new MemoryStream())
                {
                    await uploadFile.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            try
                            {
                                string slotNumber = row.Cell(1).GetString().Trim();
                                string vehicleType = row.Cell(2).GetString().Trim();
                                string isVisitorStr = row.Cell(3).GetString().Trim().ToLower();

                                if (string.IsNullOrEmpty(slotNumber) || string.IsNullOrEmpty(vehicleType))
                                {
                                    errorCount++;
                                    continue;
                                }

                                bool isVisitor = isVisitorStr == "yes" || isVisitorStr == "y" || isVisitorStr == "true" || isVisitorStr == "1";

                                var slot = new ParkingSlot
                                {
                                    SlotNumber = slotNumber,
                                    VehicleType = vehicleType,
                                    IsVisitorSlot = isVisitor,
                                    IsActive = true
                                };

                                await _parkingRepo.UpsertParkingSlotAsync(slot);
                                successCount++;
                            }
                            catch
                            {
                                errorCount++;
                            }
                        }
                    }
                }

                string msg = $"Bulk upload completed. {successCount} slots imported successfully.";
                if (errorCount > 0)
                {
                    msg += $" {errorCount} rows failed or had missing required fields.";
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
