using SmartSociety.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartSociety.Repositories
{
    public interface IFamilyRepository
    {
        Task<IEnumerable<FamilyMember>> GetByUserIdAsync(int userId);
        Task<int> UpsertFamilyMemberAsync(FamilyMember familyMember);
        Task DeleteFamilyMemberAsync(int familyMemberId);
    }
}
