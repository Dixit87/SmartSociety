using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IPollRepository
    {
        Task<IEnumerable<Poll>> GetAllPollsAsync();
        Task<Poll?> GetPollByIdAsync(int pollId);
        Task<int> CreatePollAsync(PollUpsertViewModel model);
        Task DeletePollAsync(int pollId);
        Task MockVoteAsync(int pollId, int optionId);
    }
}
