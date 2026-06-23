using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface IStaffRepository
    {
        Task<IEnumerable<Staff>> GetAllStaffAsync();
        Task<Staff> GetStaffByIdAsync(int id);
        Task<int> SaveStaffAsync(Staff staff);
        Task ToggleStatusAsync(int id);
        
        Task AssignFlatsAsync(int staffId, string flatIds);
        Task<IEnumerable<int>> GetAssignedFlatsAsync(int staffId);
        Task LogAttendanceAsync(int staffId, string logType);
        Task<IEnumerable<StaffAttendance>> GetAttendanceHistoryAsync(int staffId);
    }

    public class StaffRepository : IStaffRepository
    {
        private readonly string _connectionString;

        public StaffRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<Staff>> GetAllStaffAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Staff>("sp_Staff_GetAll", commandType: CommandType.StoredProcedure);
        }

        public async Task<Staff> GetStaffByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<Staff>(
                "sp_Staff_GetById",
                new { StaffId = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> SaveStaffAsync(Staff staff)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@StaffId", staff.StaffId);
            parameters.Add("@FullName", staff.FullName);
            parameters.Add("@Role", staff.Role);
            parameters.Add("@ContactNumber", staff.ContactNumber);
            parameters.Add("@AadharNumber", staff.AadharNumber);
            parameters.Add("@PhotoPath", staff.PhotoPath);
            parameters.Add("@ShiftStart", staff.ShiftStart);
            parameters.Add("@ShiftEnd", staff.ShiftEnd);
            parameters.Add("@IsVerified", staff.IsVerified);
            parameters.Add("@IsActive", staff.IsActive);
            parameters.Add("@NewStaffId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_Staff_CreateOrUpdate", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@NewStaffId");
        }

        public async Task ToggleStatusAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Staff_ToggleStatus",
                new { StaffId = id },
                commandType: CommandType.StoredProcedure);
        }

        public async Task AssignFlatsAsync(int staffId, string flatIds)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Staff_AssignFlats",
                new { StaffId = staffId, FlatIds = flatIds },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<int>> GetAssignedFlatsAsync(int staffId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<int>(
                "sp_Staff_GetAssignedFlats",
                new { StaffId = staffId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task LogAttendanceAsync(int staffId, string logType)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Staff_LogAttendance",
                new { StaffId = staffId, LogType = logType },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<StaffAttendance>> GetAttendanceHistoryAsync(int staffId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<StaffAttendance>(
                "sp_Staff_GetAttendance",
                new { StaffId = staffId },
                commandType: CommandType.StoredProcedure);
        }
    }
}
