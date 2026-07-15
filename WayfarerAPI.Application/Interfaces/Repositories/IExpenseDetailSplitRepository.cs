using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IExpenseDetailSplitRepository
{
    Task InsertRangeAsync(int expenseDetailId, List<ExpenseDetailSplitDto> splits);
    Task<IEnumerable<ExpenseDetailSplit>> GetByExpenseDetailIdAsync(int expenseDetailId);
    Task DeleteByExpenseDetailIdAsync(int expenseDetailId);
}
