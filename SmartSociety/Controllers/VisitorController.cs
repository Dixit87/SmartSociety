using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartSociety.Models;
using SmartSociety.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SmartSociety.Controllers
{
    [Authorize]
    public class VisitorController : Controller
    {
        private readonly IVisitorRepository _visitorRepo;
        private readonly IFlatRepository _flatRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly IUserRepository _userRepo;
        private readonly IWebHostEnvironment _env;
        private readonly IFamilyRepository _familyRepo;
        private readonly IStaffRepository _staffRepo;

        public VisitorController(IVisitorRepository visitorRepo, IFlatRepository flatRepo, INotificationRepository notificationRepo, IUserRepository userRepo, IWebHostEnvironment env, IFamilyRepository familyRepo, IStaffRepository staffRepo)
        {
            _visitorRepo = visitorRepo;
            _flatRepo = flatRepo;
            _notificationRepo = notificationRepo;
            _userRepo = userRepo;
            _env = env;
            _familyRepo = familyRepo;
            _staffRepo = staffRepo;
        }

        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> Index()
        {
            var visitors = await _visitorRepo.GetTodayVisitorsAsync();
            
            // Dashboard Stats
            ViewBag.TotalToday = visitors.Count();
            ViewBag.Inside = visitors.Count(v => v.Status == "Inside");
            ViewBag.Exited = visitors.Count(v => v.Status == "Exited" || v.Status == "Rejected");
            ViewBag.Pending = visitors.Count(v => v.Status == "Pending");

            // Load today's deliveries for the gate console
            var deliveries = await _visitorRepo.GetTodayDeliveriesAsync();
            ViewBag.Deliveries = deliveries;

            // Load today's child safety alerts
            var childRequests = await _visitorRepo.GetTodayChildExitRequestsAsync();
            ViewBag.ChildRequests = childRequests;

            // Load active helpers with today's attendance status
            var activeStaff = await _staffRepo.GetAllStaffAsync();
            var staffWithAttendance = new List<dynamic>();
            foreach (var staff in activeStaff)
            {
                if (staff.Role == "Admin" || staff.Role == "Guard") continue;
                
                var history = await _staffRepo.GetAttendanceHistoryAsync(staff.StaffId);
                var todayAttendance = history.FirstOrDefault(h => h.Date.Date == DateTime.Today);
                staffWithAttendance.Add(new {
                    Staff = staff,
                    TodayAttendance = todayAttendance
                });
            }
            ViewBag.StaffWithAttendance = staffWithAttendance;

            return View(visitors);
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpGet]
        public async Task<IActionResult> History(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
            {
                startDate = DateTime.Today.AddDays(-7);
                endDate = DateTime.Today;
            }

            var visitors = await _visitorRepo.GetVisitorHistoryAsync(startDate.Value, endDate.Value);
            
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View("Index", visitors); // Reuse the Index view but with history data
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpGet]
        public async Task<IActionResult> GetFlatsDropdown()
        {
            var flats = await _flatRepo.GetAllFlatsAsync();
            var list = flats.Select(f => new SelectListItem {
                Text = $"{f.BlockName} - {f.FlatNumber} ({f.OwnerName ?? f.TenantName ?? "No Resident"})",
                Value = f.FlatId.ToString()
            }).ToList();
            
            return Json(list);
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Entry(Visitor visitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _visitorRepo.EntryVisitorAsync(visitor);

                    // Notify resident
                    var flat = await _flatRepo.GetFlatByIdAsync(visitor.FlatId);
                    if (flat != null)
                    {
                        int? targetUserId = flat.TenantId ?? flat.OwnerId;
                        if (targetUserId.HasValue)
                        {
                            await _notificationRepo.InsertAsync(new Notification
                            {
                                UserId = targetUserId.Value,
                                Title = "Visitor at the Gate",
                                Message = $"{visitor.FullName} ({visitor.VisitorType}) has checked in at the gate for your flat.",
                                Category = "Visitor"
                            });
                        }
                    }

                    return Json(new { success = true, message = "Visitor entry logged successfully." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Error: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "Invalid data provided." });
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id)
        {
            try
            {
                await _visitorRepo.CheckoutVisitorAsync(id);
                return Json(new { success = true, message = "Visitor checked out successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error during checkout: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                await _visitorRepo.ApproveVisitorAsync(id);

                // Notify resident
                var visitor = await _visitorRepo.GetVisitorByIdAsync(id);
                if (visitor != null)
                {
                    var flat = await _flatRepo.GetFlatByIdAsync(visitor.FlatId);
                    if (flat != null)
                    {
                        int? targetUserId = flat.TenantId ?? flat.OwnerId;
                        if (targetUserId.HasValue)
                        {
                            await _notificationRepo.InsertAsync(new Notification
                            {
                                UserId = targetUserId.Value,
                                Title = "Visitor Entry Approved",
                                Message = $"Entry of visitor {visitor.FullName} ({visitor.VisitorType}) is approved.",
                                Category = "Visitor"
                            });
                        }
                    }
                }

                return Json(new { success = true, message = "Entry approved. Visitor is now inside." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error approving visitor: " + ex.Message });
            }
        }

        [Authorize(Roles = "Admin,Guard")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                await _visitorRepo.RejectVisitorAsync(id);
                return Json(new { success = true, message = "Entry rejected. Visitor marked as exited/rejected." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error rejecting visitor: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> MyVisitors()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                if (flat != null)
                {
                    var visitors = await _visitorRepo.GetByFlatIdAsync(flat.FlatId);
                    ViewBag.Flat = flat;
                    
                    var deliveries = await _visitorRepo.GetDeliveriesByFlatIdAsync(flat.FlatId);
                    ViewBag.Deliveries = deliveries;

                    // Fetch active child safety alerts for resident
                    var childRequests = await _visitorRepo.GetChildExitRequestsByFlatIdAsync(flat.FlatId);
                    ViewBag.ChildRequests = childRequests;

                    // Fetch flat's daily helpers and their attendance status
                    var flatStaff = await _staffRepo.GetStaffByFlatIdAsync(flat.FlatId);
                    var helperList = new List<dynamic>();
                    foreach (var staff in flatStaff)
                    {
                        var history = await _staffRepo.GetAttendanceHistoryAsync(staff.StaffId);
                        var todayAttendance = history.FirstOrDefault(h => h.Date.Date == DateTime.Today);
                        helperList.Add(new {
                            Staff = staff,
                            TodayAttendance = todayAttendance
                        });
                    }
                    ViewBag.Helpers = helperList;
                    
                    return View(visitors);
                }
            }
            ViewBag.Deliveries = new List<Delivery>();
            ViewBag.ChildRequests = new List<ChildExitRequest>();
            ViewBag.Helpers = new List<dynamic>();
            return View(new List<Visitor>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> PreRegister(string fullName, string phoneNumber, string visitorType, string? vehicleNumber, string? purpose, DateTime scheduledTime)
        {
            try
            {
                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                    if (flat == null)
                    {
                        return Json(new { success = false, message = "You do not have a flat assigned to pre-register visitors." });
                    }

                    string inviteCode = new Random().Next(100000, 999999).ToString();
                    DateTime expiryDate = scheduledTime.AddHours(24);

                    var visitor = new Visitor
                    {
                        FullName = fullName,
                        PhoneNumber = phoneNumber,
                        VisitorType = visitorType,
                        VehicleNumber = vehicleNumber,
                        Purpose = purpose,
                        FlatId = flat.FlatId,
                        InTime = scheduledTime,
                        Status = "Pending",
                        InviteCode = inviteCode,
                        ExpiryDate = expiryDate
                    };

                    int visitorId = await _visitorRepo.PreRegisterVisitorWithInviteAsync(visitor);
                    return Json(new { success = true, visitorId = visitorId, inviteCode = inviteCode, message = $"Visitor pre-registered successfully! Invite code is {inviteCode}." });
                }
                return Json(new { success = false, message = "Unauthorized access." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to pre-register visitor: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> VerifyInvite(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                {
                    return Json(new { success = false, message = "Please enter an invite code." });
                }

                var visitor = await _visitorRepo.GetVisitorByInviteCodeAsync(code);
                if (visitor == null)
                {
                    return Json(new { success = false, message = "Invalid or unrecognized invite code." });
                }

                if (visitor.ExpiryDate.HasValue && visitor.ExpiryDate.Value < DateTime.Now)
                {
                    return Json(new { success = false, message = "This invite code has expired." });
                }

                if (visitor.Status == "Inside")
                {
                    return Json(new { success = false, message = "Visitor is already checked in." });
                }

                if (visitor.Status == "Exited" || visitor.Status == "Rejected")
                {
                    return Json(new { success = false, message = "Visitor invite is no longer valid." });
                }

                await _visitorRepo.ApproveVisitorAsync(visitor.VisitorId);

                // Notify resident
                var flat = await _flatRepo.GetFlatByIdAsync(visitor.FlatId);
                if (flat != null)
                {
                    int? targetUserId = flat.TenantId ?? flat.OwnerId;
                    if (targetUserId.HasValue)
                    {
                        await _notificationRepo.InsertAsync(new Notification
                        {
                            UserId = targetUserId.Value,
                            Title = "Visitor Checked In (Invite)",
                            Message = $"{visitor.FullName} ({visitor.VisitorType}) has checked in using invite code {code}.",
                            Category = "Visitor"
                        });
                    }
                }

                return Json(new { success = true, message = $"Invite verified! {visitor.FullName} has been checked in." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error verifying invite: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> LogDelivery(int flatId, string company, string deliveryAgentName, string deliveryAgentPhone, IFormFile? receiptPhoto)
        {
            try
            {
                if (flatId <= 0 || string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(deliveryAgentName) || string.IsNullOrWhiteSpace(deliveryAgentPhone))
                {
                    return Json(new { success = false, message = "All required fields must be completed." });
                }

                string? receiptPhotoPath = null;

                if (receiptPhoto != null && receiptPhoto.Length > 0)
                {
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(receiptPhoto.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Invalid photo format. Only .pdf, .jpg, .jpeg, .png are allowed." });
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "deliveries");
                    if (!System.IO.Directory.Exists(uploadsFolder))
                    {
                        System.IO.Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(receiptPhoto.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receiptPhoto.CopyToAsync(fileStream);
                    }

                    receiptPhotoPath = "/uploads/deliveries/" + uniqueFileName;
                }

                var delivery = new Delivery
                {
                    FlatId = flatId,
                    Company = company,
                    DeliveryAgentName = deliveryAgentName,
                    DeliveryAgentPhone = deliveryAgentPhone,
                    ReceiptPhoto = receiptPhotoPath,
                    Status = "LoggedAtGate"
                };

                await _visitorRepo.InsertDeliveryAsync(delivery);

                // Notify Resident
                var flat = await _flatRepo.GetFlatByIdAsync(flatId);
                if (flat != null)
                {
                    int? targetUserId = flat.TenantId ?? flat.OwnerId;
                    if (targetUserId.HasValue)
                    {
                        await _notificationRepo.InsertAsync(new Notification
                        {
                            UserId = targetUserId.Value,
                            Title = "New Parcel at the Gate",
                            Message = $"A parcel from {company} has been dropped off at the gate by agent {deliveryAgentName}.",
                            Category = "Visitor"
                        });
                    }
                }

                return Json(new { success = true, message = "Delivery logged and resident notified successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to log delivery: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> CollectDelivery(int id)
        {
            try
            {
                var delivery = await _visitorRepo.GetDeliveryByIdAsync(id);
                if (delivery == null)
                {
                    return Json(new { success = false, message = "Delivery record not found." });
                }

                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                    if (flat == null || flat.FlatId != delivery.FlatId)
                    {
                        return Json(new { success = false, message = "Unauthorized to collect this delivery." });
                    }

                    await _visitorRepo.CollectDeliveryAsync(id);
                    return Json(new { success = true, message = "Delivery marked as collected." });
                }
                return Json(new { success = false, message = "Unauthorized access." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to collect delivery: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> Directory()
        {
            var flats = await _flatRepo.GetAllFlatsAsync();
            return View(flats);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> GetChildrenForFlat(int flatId)
        {
            try
            {
                var flat = await _flatRepo.GetFlatByIdAsync(flatId);
                if (flat == null)
                {
                    return Json(new { success = false, message = "Flat not found." });
                }

                int? residentId = flat.TenantId ?? flat.OwnerId;
                if (!residentId.HasValue)
                {
                    return Json(new { success = true, data = new List<FamilyMember>() });
                }

                var family = await _familyRepo.GetByUserIdAsync(residentId.Value);
                var children = family.Where(f => string.Equals(f.Relation, "Child", StringComparison.OrdinalIgnoreCase)).ToList();
                return Json(new { success = true, data = children });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> LogChildExitRequest(int flatId, int familyMemberId, string? guardRemarks)
        {
            try
            {
                if (flatId <= 0 || familyMemberId <= 0)
                {
                    return Json(new { success = false, message = "Please select both Flat and Child." });
                }

                var flat = await _flatRepo.GetFlatByIdAsync(flatId);
                if (flat == null)
                {
                    return Json(new { success = false, message = "Flat not found." });
                }

                int? residentId = flat.TenantId ?? flat.OwnerId;
                if (!residentId.HasValue)
                {
                    return Json(new { success = false, message = "This flat has no registered active resident." });
                }

                var family = await _familyRepo.GetByUserIdAsync(residentId.Value);
                var child = family.FirstOrDefault(f => f.FamilyMemberId == familyMemberId);
                if (child == null)
                {
                    return Json(new { success = false, message = "Selected child does not match the flat resident." });
                }

                var request = new ChildExitRequest
                {
                    FlatId = flatId,
                    FamilyMemberId = familyMemberId,
                    Status = "Pending",
                    GuardRemarks = guardRemarks
                };

                int requestId = await _visitorRepo.InsertChildExitRequestAsync(request);

                // Notify Resident (Parent)
                await _notificationRepo.InsertAsync(new Notification
                {
                    UserId = residentId.Value,
                    Title = "Child Gate Exit Alert ⚠️",
                    Message = $"Your child {child.FullName} is trying to exit the society gate alone. Permission required!",
                    Category = "Visitor"
                });

                return Json(new { success = true, requestId = requestId, message = $"Exit request logged. Parent ({flat.BlockName}-{flat.FlatNumber}) alert sent." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to log request: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Resident")]
        public async Task<IActionResult> ActionChildExitRequest(int id, string status)
        {
            try
            {
                if (status != "Approved" && status != "Rejected")
                {
                    return Json(new { success = false, message = "Invalid request status." });
                }

                var request = await _visitorRepo.GetChildExitRequestByIdAsync(id);
                if (request == null)
                {
                    return Json(new { success = false, message = "Exit request record not found." });
                }

                var userIdStr = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdStr, out int userId))
                {
                    var flat = await _flatRepo.GetFlatByUserIdAsync(userId);
                    if (flat == null || flat.FlatId != request.FlatId)
                    {
                        return Json(new { success = false, message = "Unauthorized to approve exit for this child." });
                    }

                    await _visitorRepo.UpdateChildExitRequestStatusAsync(id, status);

                    // Notify resident
                    await _notificationRepo.InsertAsync(new Notification
                    {
                        UserId = userId,
                        Title = $"Child Exit Permission {status}",
                        Message = $"You have {status.ToLower()} exit permission for {request.ChildName}.",
                        Category = "Visitor"
                    });

                    return Json(new { success = true, message = $"Exit permission has been {status.ToLower()}." });
                }
                return Json(new { success = false, message = "Unauthorized access." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error actioning request: " + ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Guard")]
        public async Task<IActionResult> GetResidentContacts()
        {
            var flats = await _flatRepo.GetAllFlatsAsync();
            var users = await _userRepo.GetAllUsersAsync();
            var userDict = users.ToDictionary(u => u.UserId);
            var contacts = new Dictionary<string, string>();
            foreach (var flat in flats)
            {
                int? residentId = flat.TenantId ?? flat.OwnerId;
                if (residentId.HasValue && userDict.TryGetValue(residentId.Value, out var user))
                {
                    contacts[$"{flat.BlockName}-{flat.FlatNumber}"] = user.PhoneNumber;
                }
            }
            return Json(new { success = true, contacts = contacts });
        }
    }
}
