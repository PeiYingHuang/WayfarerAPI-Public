using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IExpenseDetailRepository
{
    Task<List<int>> InsertRangeAsync(Guid expenseId, List<ExpenseDetailRequestDto> details);
    Task<IEnumerable<ExpenseDetail>> GetByExpenseIdAsync(Guid expenseId);
    Task DeleteByExpenseIdAsync(Guid expenseId);
}
