using SmartSociety.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface ISettingRepository
    {
        Task<SystemSetting?> GetSettingsAsync();
        Task UpdateSettingsAsync(SystemSetting settings);
        Task<IEnumerable<DatabaseBackup>> GetAllBackupsAsync();
        Task InsertBackupAsync(string fileName, string filePath, decimal sizeMb);
    }
}
