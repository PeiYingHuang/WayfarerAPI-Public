using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Application.Interfaces.Repositories;

public interface IExpenseRepository
{
    Task<Guid> InsertAsync(Expense expense);
    Task<IEnumerable<Expense>> GetByTravelIdAsync(Guid travelId);
    Task<Expense?> GetByIdAsync(Guid expenseId);
    Task UpdateAsync(Guid expenseId, Guid payerMemberId, string? itemName, decimal consumptionAmount, string currencyCode, decimal exchangeRate, decimal settlementAmount, string? category, string? note, DateTime? expenseTime);
    Task DeleteAsync(Guid travellerId, Guid expenseId);
}