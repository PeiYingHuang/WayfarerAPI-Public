using WayfarerAPI.Application.Models;

namespace WayfarerAPI.Application.Interfaces.QueryServices
{
    public interface IExpenseQueryService
    {
        Task<IEnumerable<ExpenseInfoModel>> GetExpenseInfoByMemberIdAsync(Guid memberId);
    }
}