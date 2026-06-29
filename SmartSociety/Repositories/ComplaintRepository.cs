using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly string _connectionString;

        public ComplaintRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<Complaint>> GetAllAsync(string status = null, int? month = null, int? year = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Complaint>(
                "sp_Complaint_GetAll",
                new { Status = status, Month = month, Year = year },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Complaint> GetByIdAsync(int complaintId)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<Complaint>(
                "sp_Complaint_GetById",
                new { ComplaintId = complaintId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateAsync(Complaint complaint)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", complaint.FlatId);
            parameters.Add("@RaisedBy", complaint.RaisedBy);
            parameters.Add("@Category", complaint.Category);
            parameters.Add("@Title", complaint.Title);
            parameters.Add("@Description", complaint.Description);
            parameters.Add("@Priority", complaint.Priority);
            parameters.Add("@PhotoUrl", complaint.PhotoUrl);
            parameters.Add("@ComplaintId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_Complaint_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@ComplaintId");
        }

        public async Task UpdateStatusAsync(int complaintId, string status, string adminRemarks = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Complaint_UpdateStatus",
                new { ComplaintId = complaintId, Status = status, AdminRemarks = adminRemarks },
                commandType: CommandType.StoredProcedure);
        }

        public async Task AssignAsync(int complaintId, int assignedTo)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Complaint_Assign",
                new { ComplaintId = complaintId, AssignedTo = assignedTo },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<ComplaintDashboardStats> GetDashboardStatsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<ComplaintDashboardStats>(
                "sp_Complaint_GetDashboardStats",
                commandType: CommandType.StoredProcedure) ?? new ComplaintDashboardStats();
        }

        public async Task UpdateAsync(Complaint complaint)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Complaint_Update",
                new
                {
                    ComplaintId = complaint.ComplaintId,
                    Category = complaint.Category,
                    Title = complaint.Title,
                    Description = complaint.Description,
                    Priority = complaint.Priority
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Complaint>> GetByFlatIdAsync(int flatId)
        {
            using var connection = new SqlConnection(_connectionString);
            string query = @"
                SELECT 
                    c.ComplaintId,
                    c.FlatId,
                    b.BlockName,
                    f.FlatNumber,
                    c.RaisedBy,
                    u1.FullName AS ResidentName,
                    c.Category,
                    c.Title,
                    c.Description,
                    c.Priority,
                    c.Status,
                    c.AssignedTo,
                    u2.FullName AS TechnicianName,
                    c.AdminRemarks,
                    c.PhotoUrl,
                    c.CreatedAt,
                    c.UpdatedAt,
                    c.ResolvedAt
                FROM Complaints c
                INNER JOIN Flats f ON c.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                INNER JOIN Users u1 ON c.RaisedBy = u1.UserId
                LEFT JOIN Users u2 ON c.AssignedTo = u2.UserId
                WHERE c.FlatId = @FlatId
                ORDER BY c.CreatedAt DESC;";
            return await connection.QueryAsync<Complaint>(query, new { FlatId = flatId });
        }

        public async Task DeleteAsync(int complaintId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Complaint_Delete",
                new { ComplaintId = complaintId },
                commandType: CommandType.StoredProcedure);
        }
    }
}
