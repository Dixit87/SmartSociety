using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class NoticeRepository : INoticeRepository
    {
        private readonly IDbConnection _dbConnection;

        public NoticeRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Notice>> GetAllNoticesAsync(string? status = null, string? category = null)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Status", status);
            parameters.Add("@Category", category);

            return await _dbConnection.QueryAsync<Notice>(
                "sp_Notices_GetAll",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Notice?> GetNoticeByIdAsync(int noticeId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@NoticeId", noticeId);

            return await _dbConnection.QueryFirstOrDefaultAsync<Notice>(
                "sp_Notices_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertNoticeAsync(Notice notice)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@NoticeId", notice.NoticeId);
            parameters.Add("@Title", notice.Title);
            parameters.Add("@Description", notice.Description);
            parameters.Add("@Category", notice.Category);
            parameters.Add("@TargetAudience", notice.TargetAudience);
            parameters.Add("@AttachmentPath", notice.AttachmentPath);
            parameters.Add("@IsPinned", notice.IsPinned);
            parameters.Add("@ValidFrom", notice.ValidFrom);
            parameters.Add("@ValidTill", notice.ValidTill);
            parameters.Add("@Status", notice.Status);
            parameters.Add("@CreatedBy", notice.CreatedBy);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Notices_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteNoticeAsync(int noticeId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@NoticeId", noticeId);

            await _dbConnection.ExecuteAsync(
                "sp_Notices_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task TogglePinStatusAsync(int noticeId, bool isPinned)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@NoticeId", noticeId);
            parameters.Add("@IsPinned", isPinned);

            await _dbConnection.ExecuteAsync(
                "sp_Notices_TogglePin",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
