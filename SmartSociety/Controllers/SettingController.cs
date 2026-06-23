using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartSociety.Models;
using SmartSociety.Repositories;

namespace SmartSociety.Controllers
{
    public class SettingController : Controller
    {
        private readonly ISettingRepository _repository;
        private readonly IWebHostEnvironment _env;
        private readonly string _connectionString;

        public SettingController(ISettingRepository repository, IWebHostEnvironment env, IConfiguration configuration)
        {
            _repository = repository;
            _env = env;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new SettingsViewModel
            {
                Settings = await _repository.GetSettingsAsync() ?? new SystemSetting(),
                Backups = (await _repository.GetAllBackupsAsync()).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(SystemSetting settings)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _repository.UpdateSettingsAsync(settings);
                    return Json(new { success = true, message = "Settings updated successfully!" });
                }
                return Json(new { success = false, message = "Invalid data provided." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating settings: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBackup()
        {
            try
            {
                string backupsFolder = Path.Combine(_env.WebRootPath, "backups");
                if (!Directory.Exists(backupsFolder))
                {
                    Directory.CreateDirectory(backupsFolder);
                }

                string fileName = $"SmartSociety_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                string fullPath = Path.Combine(backupsFolder, fileName);

                // Execute SQL Backup Command
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Backup command
                    string backupQuery = $"BACKUP DATABASE [SmartSociety] TO DISK = @path WITH FORMAT, MEDIANAME = 'DBBackup', NAME = 'Full Backup'";

                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        command.Parameters.AddWithValue("@path", fullPath);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Check if file exists to get size
                if (System.IO.File.Exists(fullPath))
                {
                    FileInfo fileInfo = new FileInfo(fullPath);
                    decimal sizeMb = Math.Round((decimal)fileInfo.Length / (1024 * 1024), 2);
                    string relativePath = $"/backups/{fileName}";

                    // Save record
                    await _repository.InsertBackupAsync(fileName, relativePath, sizeMb);

                    return Json(new { success = true, message = "Database backup completed successfully!", fileName = fileName });
                }
                else
                {
                    return Json(new { success = false, message = "Backup command executed, but file not found. Check SQL Server folder permissions." });
                }
            }
            catch (Exception ex)
            {
                // Give a friendly error if it's a permission issue
                if (ex.Message.Contains("Operating system error") || ex.Message.Contains("Access is denied"))
                {
                    return Json(new { success = false, message = "SQL Server does not have permission to write to the 'wwwroot/backups' folder. Please run SQL Server as Administrator or give it write access to this folder." });
                }
                return Json(new { success = false, message = "Error creating backup: " + ex.Message });
            }
        }
    }
}
