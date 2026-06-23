using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class SettingRepository : ISettingRepository
    {
        private readonly IDbConnection _dbConnection;

        public SettingRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<SystemSetting?> GetSettingsAsync()
        {
            return await _dbConnection.QueryFirstOrDefaultAsync<SystemSetting>(
                "sp_Settings_Get",
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateSettingsAsync(SystemSetting settings)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SocietyName", settings.SocietyName);
            parameters.Add("@RegistrationNo", settings.RegistrationNo);
            parameters.Add("@Address", settings.Address);
            parameters.Add("@ContactEmail", settings.ContactEmail);
            parameters.Add("@ContactPhone", settings.ContactPhone);
            parameters.Add("@CurrencySymbol", settings.CurrencySymbol);
            parameters.Add("@DefaultPenaltyPercentage", settings.DefaultPenaltyPercentage);

            await _dbConnection.ExecuteAsync(
                "sp_Settings_Update",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<DatabaseBackup>> GetAllBackupsAsync()
        {
            return await _dbConnection.QueryAsync<DatabaseBackup>(
                "sp_Backups_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task InsertBackupAsync(string fileName, string filePath, decimal sizeMb)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FileName", fileName);
            parameters.Add("@FilePath", filePath);
            parameters.Add("@SizeMB", sizeMb);

            await _dbConnection.ExecuteAsync(
                "sp_Backups_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
