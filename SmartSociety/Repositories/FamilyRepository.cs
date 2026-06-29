using Dapper;
using SmartSociety.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public class FamilyRepository : IFamilyRepository
    {
        private readonly IDbConnection _dbConnection;

        public FamilyRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<FamilyMember>> GetByUserIdAsync(int userId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@UserId", userId);

            return await _dbConnection.QueryAsync<FamilyMember>(
                "sp_FamilyMembers_GetByUserId",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertFamilyMemberAsync(FamilyMember familyMember)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FamilyMemberId", familyMember.FamilyMemberId);
            parameters.Add("@UserId", familyMember.UserId);
            parameters.Add("@FullName", familyMember.FullName);
            parameters.Add("@Relation", familyMember.Relation);
            parameters.Add("@PhoneNumber", familyMember.PhoneNumber);
            parameters.Add("@Email", familyMember.Email);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_FamilyMembers_Upsert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteFamilyMemberAsync(int familyMemberId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FamilyMemberId", familyMemberId);

            await _dbConnection.ExecuteAsync(
                "sp_FamilyMembers_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
