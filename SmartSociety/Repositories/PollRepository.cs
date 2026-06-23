using Dapper;
using SmartSociety.Models;
using System.Data;
using System.Linq;

namespace SmartSociety.Repositories
{
    public class PollRepository : IPollRepository
    {
        private readonly IDbConnection _dbConnection;

        public PollRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<Poll>> GetAllPollsAsync()
        {
            return await _dbConnection.QueryAsync<Poll>(
                "sp_Polls_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<Poll?> GetPollByIdAsync(int pollId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@PollId", pollId);

            using var multi = await _dbConnection.QueryMultipleAsync(
                "sp_Polls_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);

            var poll = await multi.ReadFirstOrDefaultAsync<Poll>();
            if (poll != null)
            {
                poll.Options = (await multi.ReadAsync<PollOption>()).ToList();
                poll.TotalVotes = poll.Options.Sum(o => o.VoteCount);

                // Calculate percentages for UI
                if (poll.TotalVotes > 0)
                {
                    foreach (var opt in poll.Options)
                    {
                        opt.VotePercentage = Math.Round(((double)opt.VoteCount / poll.TotalVotes) * 100, 1);
                    }
                }
            }

            return poll;
        }

        public async Task<int> CreatePollAsync(PollUpsertViewModel model)
        {
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            using var transaction = _dbConnection.BeginTransaction();
            try
            {
                var pParams = new DynamicParameters();
                pParams.Add("@PollId", model.PollId);
                pParams.Add("@Question", model.Question);
                pParams.Add("@Description", model.Description);
                pParams.Add("@EndDate", model.EndDate);

                int pollId = await _dbConnection.QuerySingleAsync<int>(
                    "sp_Polls_Upsert",
                    pParams,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure);

                // If it's a new poll, insert options
                if (model.PollId == 0 && model.Options != null && model.Options.Any())
                {
                    foreach (var optionText in model.Options.Where(o => !string.IsNullOrWhiteSpace(o)))
                    {
                        var optParams = new DynamicParameters();
                        optParams.Add("@PollId", pollId);
                        optParams.Add("@OptionText", optionText.Trim());

                        await _dbConnection.ExecuteAsync(
                            "sp_PollOptions_Insert",
                            optParams,
                            transaction: transaction,
                            commandType: CommandType.StoredProcedure);
                    }
                }

                transaction.Commit();
                return pollId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task DeletePollAsync(int pollId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@PollId", pollId);

            await _dbConnection.ExecuteAsync(
                "sp_Polls_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task MockVoteAsync(int pollId, int optionId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@PollId", pollId);
            parameters.Add("@OptionId", optionId);

            await _dbConnection.ExecuteAsync(
                "sp_Polls_MockVote",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
