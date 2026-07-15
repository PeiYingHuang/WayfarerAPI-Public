using Dapper;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Application.Models;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public class ExpenseSplitRepository : IExpenseSplitRepository
{
    private readonly IDbSession _session;

    public ExpenseSplitRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task InsertRangeAsync(Guid expenseId, List<ExpenseSplitRequestDto> splits)
    {
        if (splits.Count == 0)
            return;

        const string sql = """
            INSERT INTO ExpenseSplit (Id, ExpenseId, MemberId, SplitAmount)
            VALUES (@Id, @ExpenseId, @MemberId, @SplitAmount);
            """;

        foreach (var s in splits)
        {
            await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                Id = Guid.CreateVersion7(),
                ExpenseId = expenseId,
                s.MemberId,
                s.SplitAmount
            }, transaction: _session.Transaction));
        }
    }

    public async Task<IEnumerable<ExpenseSplitModel>> GetByTravelIdAsync(Guid travelId)
    {
        const string sql = """
            SELECT 
                es.Id,
                es.ExpenseId,
                es.MemberId,
                es.SplitAmount,
                e.PayerMemberId
            FROM ExpenseSplit AS es
                INNER JOIN Expense AS e ON es.ExpenseId = e.Id
            WHERE e.TravelId = @TravelId;
            """;

        return await _session.Connection.QueryAsync<ExpenseSplitModel>(new CommandDefinition(sql, new { TravelId = travelId }));
    }

    public async Task<IEnumerable<ExpenseSplit>> GetByExpenseIdAsync(Guid expenseId)
    {
        const string sql = """
            SELECT 
                Id,
                ExpenseId,
                MemberId,
                SplitAmount
            FROM ExpenseSplit
            WHERE ExpenseId = @ExpenseId;
            """;

        return await _session.Connection.QueryAsync<ExpenseSplit>(new CommandDefinition(sql, new { ExpenseId = expenseId }));
    }

    public async Task DeleteByExpenseIdAsync(Guid expenseId)
    {
        const string sql = "DELETE FROM ExpenseSplit WHERE ExpenseId = @ExpenseId;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { ExpenseId = expenseId }, transaction: _session.Transaction));
    }
}
