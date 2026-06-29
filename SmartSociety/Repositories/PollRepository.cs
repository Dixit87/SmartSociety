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
            var polls = (await _dbConnection.QueryAsync<Poll>(
                "sp_Polls_GetAll",
                commandType: CommandType.StoredProcedure)).ToList();

            if (polls.Any())
            {
                var pollIds = polls.Select(p => p.PollId).ToList();
                var options = (await _dbConnection.QueryAsync<PollOption>(
                    "SELECT o.OptionId, o.PollId, o.OptionText, " +
                    "(SELECT COUNT(*) FROM PollVotes v WHERE v.OptionId = o.OptionId) AS VoteCount " +
                    "FROM PollOptions o " +
                    "WHERE o.PollId IN @PollIds",
                    new { PollIds = pollIds })).ToList();

                foreach (var poll in polls)
                {
                    poll.Options = options.Where(o => o.PollId == poll.PollId).ToList();
                    poll.TotalVotes = poll.Options.Sum(o => o.VoteCount);
                    if (poll.TotalVotes > 0)
                    {
                        foreach (var opt in poll.Options)
                        {
                            opt.VotePercentage = Math.Round(((double)opt.VoteCount / poll.TotalVotes) * 100, 1);
                        }
                    }
                }
            }

            return polls;
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


        public async Task<Dictionary<int, int>> GetUserVotesAsync(int userId)
        {
            var votes = await _dbConnection.QueryAsync<UserVoteDto>(
                "SELECT PollId, OptionId FROM PollVotes WHERE UserId = @UserId",
                new { UserId = userId });

            return votes.ToDictionary(v => v.PollId, v => v.OptionId);
        }

        public async Task<bool> CastVoteAsync(int pollId, int optionId, int userId)
        {
            var count = await _dbConnection.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM PollVotes WHERE PollId = @PollId AND UserId = @UserId",
                new { PollId = pollId, UserId = userId });

            if (count > 0)
            {
                return false;
            }

            await _dbConnection.ExecuteAsync(
                "INSERT INTO PollVotes (PollId, OptionId, UserId, VotedAt) VALUES (@PollId, @OptionId, @UserId, GETDATE())",
                new { PollId = pollId, OptionId = optionId, UserId = userId });

            return true;
        }

        private class UserVoteDto
        {
            public int PollId { get; set; }
            public int OptionId { get; set; }
        }
    }
}
