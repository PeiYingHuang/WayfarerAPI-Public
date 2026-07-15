using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IExpenseSplitRepository
{
    Task InsertRangeAsync(Guid expenseId, List<ExpenseSplitRequestDto> splits);
    Task<IEnumerable<ExpenseSplitModel>> GetByTravelIdAsync(Guid travelId);
    Task<IEnumerable<ExpenseSplit>> GetByExpenseIdAsync(Guid expenseId);
    Task DeleteByExpenseIdAsync(Guid expenseId);
}
