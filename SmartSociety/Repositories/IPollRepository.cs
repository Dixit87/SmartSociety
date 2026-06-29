using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IPollRepository
    {
        Task<IEnumerable<Poll>> GetAllPollsAsync();
        Task<Poll?> GetPollByIdAsync(int pollId);
        Task<int> CreatePollAsync(PollUpsertViewModel model);
        Task DeletePollAsync(int pollId);
        Task<Dictionary<int, int>> GetUserVotesAsync(int userId);
        Task<bool> CastVoteAsync(int pollId, int optionId, int userId);
    }
}
