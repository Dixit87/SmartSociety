using SmartSociety.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface IForumRepository
    {
        Task<IEnumerable<ForumTopic>> GetAllTopicsAsync();
        Task<ForumTopic?> GetTopicByIdAsync(int topicId);
        Task<int> SaveTopicAsync(ForumTopic topic);
        Task DeleteTopicAsync(int topicId);
        Task<IEnumerable<ForumReply>> GetRepliesByTopicIdAsync(int topicId);
        Task<int> InsertReplyAsync(ForumReply reply);
        Task DeleteReplyAsync(int replyId);
    }
}
