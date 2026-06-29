using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class ForumRepository : IForumRepository
    {
        private readonly IDbConnection _dbConnection;

        public ForumRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<ForumTopic>> GetAllTopicsAsync()
        {
            return await _dbConnection.QueryAsync<ForumTopic>(
                "sp_ForumTopics_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<ForumTopic?> GetTopicByIdAsync(int topicId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TopicId", topicId);

            return await _dbConnection.QueryFirstOrDefaultAsync<ForumTopic>(
                "sp_ForumTopics_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> SaveTopicAsync(ForumTopic topic)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TopicId", topic.TopicId);
            parameters.Add("@UserId", topic.UserId);
            parameters.Add("@Title", topic.Title);
            parameters.Add("@Content", topic.Content);
            parameters.Add("@Category", topic.Category);
            parameters.Add("@IsPinned", topic.IsPinned);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_ForumTopics_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteTopicAsync(int topicId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TopicId", topicId);

            await _dbConnection.ExecuteAsync(
                "sp_ForumTopics_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<ForumReply>> GetRepliesByTopicIdAsync(int topicId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TopicId", topicId);

            return await _dbConnection.QueryAsync<ForumReply>(
                "sp_ForumReplies_GetByTopicId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertReplyAsync(ForumReply reply)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@TopicId", reply.TopicId);
            parameters.Add("@UserId", reply.UserId);
            parameters.Add("@Content", reply.Content);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_ForumReplies_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteReplyAsync(int replyId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@ReplyId", replyId);

            await _dbConnection.ExecuteAsync(
                "sp_ForumReplies_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
