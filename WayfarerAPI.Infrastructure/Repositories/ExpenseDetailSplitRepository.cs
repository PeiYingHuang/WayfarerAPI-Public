using Dapper;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public class ExpenseDetailSplitRepository : IExpenseDetailSplitRepository
{
    private readonly IDbSession _session;

    public ExpenseDetailSplitRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task InsertRangeAsync(int expenseDetailId, List<ExpenseDetailSplitDto> splits)
    {
        if (splits == null || splits.Count == 0)
            return;

        var transaction = _session.Transaction;
        var connection = transaction?.Connection ?? _session.Connection;

        const string sql = """
            INSERT INTO ExpenseDetailSplit (ExpenseDetailId, MemberId, SplitAmount)
            VALUES (@ExpenseDetailId, @MemberId, @SplitAmount);
            """;

        foreach (var s in splits)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, new
            {
                ExpenseDetailId = expenseDetailId,
                s.MemberId,
                s.SplitAmount
            }, transaction: transaction));
        }
    }

    public async Task<IEnumerable<ExpenseDetailSplit>> GetByExpenseDetailIdAsync(int expenseDetailId)
    {
        var transaction = _session.Transaction;
        var connection = transaction?.Connection ?? _session.Connection;

        const string sql = """
            SELECT
                Id,
                ExpenseDetailId,
                MemberId,
                SplitAmount
            FROM ExpenseDetailSplit
            WHERE ExpenseDetailId = @ExpenseDetailId;
            """;

        return await connection.QueryAsync<ExpenseDetailSplit>(new CommandDefinition(sql, new { ExpenseDetailId = expenseDetailId }, transaction: transaction));
    }

    public async Task DeleteByExpenseDetailIdAsync(int expenseDetailId)
    {
        var transaction = _session.Transaction;
        var connection = transaction?.Connection ?? _session.Connection;

        const string sql = "DELETE FROM ExpenseDetailSplit WHERE ExpenseDetailId = @ExpenseDetailId;";
        await connection.ExecuteAsync(new CommandDefinition(sql, new { ExpenseDetailId = expenseDetailId }, transaction: transaction));
    }
}
