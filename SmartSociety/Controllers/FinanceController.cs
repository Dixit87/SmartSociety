using System;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartSociety.Models;
using SmartSociety.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace SmartSociety.Controllers
{
    [Authorize(Roles = "Admin,Accountant")]
    public class FinanceController : Controller
    {
        private readonly IFinanceRepository _financeRepo;
        private readonly IWebHostEnvironment _env;

        public FinanceController(IFinanceRepository financeRepo, IWebHostEnvironment env)
        {
            _financeRepo = financeRepo;
            _env = env;
        }

        public async Task<IActionResult> Index(int? month = null, int? year = null)
        {
            if (!month.HasValue) month = DateTime.Now.Month;
            if (!year.HasValue) year = DateTime.Now.Year;

            var stats = await _financeRepo.GetDashboardStatsAsync(month, year);
            var chartData = await _financeRepo.GetExpenseChartDataAsync(month, year);
            var expenses = await _financeRepo.GetExpensesAsync(month, year);
            var incomes = await _financeRepo.GetOtherIncomesAsync(month, year);
            var categories = await _financeRepo.GetExpenseCategoriesAsync();

            ViewBag.CurrentMonth = month;
            ViewBag.CurrentYear = year;

            dynamic model = new ExpandoObject();
            model.Stats = stats;
            model.ChartData = chartData;
            model.Expenses = expenses;
            model.Incomes = incomes;
            model.Categories = categories;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(int categoryId, string title, decimal amount, DateTime dateIncurred, string paidTo, string paymentMethod, string referenceNo, string notes, IFormFile receipt)
        {
            try
            {
                string receiptUrl = null;
                if (receipt != null && receipt.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(receipt.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only PDF and image files (.pdf, .jpg, .jpeg, .png) are allowed for receipt." });
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + receipt.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receipt.CopyToAsync(fileStream);
                    }
                    receiptUrl = "/uploads/receipts/" + uniqueFileName;
                }

                var expense = new Expense
                {
                    CategoryId = categoryId,
                    Title = title,
                    Amount = amount,
                    DateIncurred = dateIncurred,
                    PaidTo = paidTo,
                    PaymentMethod = paymentMethod,
                    ReferenceNo = referenceNo,
                    ReceiptUrl = receiptUrl,
                    Notes = notes
                };

                await _financeRepo.CreateExpenseAsync(expense);
                return Json(new { success = true, message = "Expense recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to record expense: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(int expenseId, int categoryId, string title, decimal amount, DateTime dateIncurred, string paidTo, string paymentMethod, string referenceNo, string notes)
        {
            try
            {
                var expense = new Expense
                {
                    ExpenseId = expenseId,
                    CategoryId = categoryId,
                    Title = title,
                    Amount = amount,
                    DateIncurred = dateIncurred,
                    PaidTo = paidTo,
                    PaymentMethod = paymentMethod,
                    ReferenceNo = referenceNo,
                    Notes = notes
                };

                await _financeRepo.UpdateExpenseAsync(expense);
                return Json(new { success = true, message = "Expense updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update expense: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int expenseId)
        {
            try
            {
                await _financeRepo.DeleteExpenseAsync(expenseId);
                return Json(new { success = true, message = "Expense deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to delete expense: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOtherIncome(string source, string title, decimal amount, DateTime dateReceived, string receivedFrom, string paymentMethod, string referenceNo, string notes, IFormFile receipt)
        {
            try
            {
                string receiptUrl = null;
                if (receipt != null && receipt.Length > 0)
                {
                    // Security: Validate file extension
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(receipt.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Json(new { success = false, message = "Only PDF and image files (.pdf, .jpg, .jpeg, .png) are allowed for receipt." });
                    }

                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "receipts");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + receipt.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await receipt.CopyToAsync(fileStream);
                    }
                    receiptUrl = "/uploads/receipts/" + uniqueFileName;
                }

                var income = new OtherIncome
                {
                    Source = source,
                    Title = title,
                    Amount = amount,
                    DateReceived = dateReceived,
                    ReceivedFrom = receivedFrom,
                    PaymentMethod = paymentMethod,
                    ReferenceNo = referenceNo,
                    ReceiptUrl = receiptUrl,
                    Notes = notes
                };

                await _financeRepo.CreateOtherIncomeAsync(income);
                return Json(new { success = true, message = "Income recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to record income: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOtherIncome(int incomeId, string source, string title, decimal amount, DateTime dateReceived, string receivedFrom, string paymentMethod, string referenceNo, string notes)
        {
            try
            {
                var income = new OtherIncome
                {
                    IncomeId = incomeId,
                    Source = source,
                    Title = title,
                    Amount = amount,
                    DateReceived = dateReceived,
                    ReceivedFrom = receivedFrom,
                    PaymentMethod = paymentMethod,
                    ReferenceNo = referenceNo,
                    Notes = notes
                };

                await _financeRepo.UpdateOtherIncomeAsync(income);
                return Json(new { success = true, message = "Income updated successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update income: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOtherIncome(int incomeId)
        {
            try
            {
                await _financeRepo.DeleteOtherIncomeAsync(incomeId);
                return Json(new { success = true, message = "Income deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to delete income: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SinkingFund()
        {
            var fds = await _financeRepo.GetFixedDepositsAsync();
            var txns = await _financeRepo.GetSinkingFundTransactionsAsync();
            var balance = await _financeRepo.GetSinkingFundBalanceAsync();

            dynamic model = new ExpandoObject();
            model.FixedDeposits = fds;
            model.Transactions = txns;
            model.SinkingFundBalance = balance;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFixedDeposit(FixedDeposit fd)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _financeRepo.CreateFixedDepositAsync(fd);
                    return Json(new { success = true, message = "Fixed Deposit created successfully." });
                }
                return Json(new { success = false, message = "Invalid data model." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to create Fixed Deposit: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFdStatus(int fdId, string status)
        {
            try
            {
                await _financeRepo.UpdateFixedDepositStatusAsync(fdId, status);
                return Json(new { success = true, message = $"Fixed Deposit status updated to {status}." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to update Fixed Deposit status: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordSinkingFundTransaction(string type, decimal amount, string purpose, string? referenceId)
        {
            try
            {
                var txn = new SinkingFundTransaction
                {
                    Type = type,
                    Amount = amount,
                    Purpose = purpose,
                    ReferenceId = referenceId,
                    TransactionDate = DateTime.Now
                };
                await _financeRepo.CreateSinkingFundTransactionAsync(txn);
                return Json(new { success = true, message = "Sinking Fund transaction recorded successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Failed to record Sinking Fund transaction: " + ex.Message });
            }
        }
    }
}
