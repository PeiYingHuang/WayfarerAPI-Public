using Dapper;
using WayfarerAPI.Application.DTOs;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public class ExpenseDetailRepository : IExpenseDetailRepository
{
    private readonly IDbSession _session;

    public ExpenseDetailRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<List<int>> InsertRangeAsync(Guid expenseId, List<ExpenseDetailRequestDto> details)
    {
        if (details.Count == 0)
            return new List<int>();

        var insertedIds = new List<int>();
        var transaction = _session.Transaction;
        var connection = transaction?.Connection ?? _session.Connection;

        const string insertSql = """
            INSERT INTO ExpenseDetail (ExpenseId, Item, UnitPrice, Quantity, Amount, Description)
            VALUES (@ExpenseId, @Item, @UnitPrice, @Quantity, @Amount, @Description);
            """;

        const string lastIdSql = "SELECT LAST_INSERT_ID();";

        foreach (var d in details)
        {
            await connection.ExecuteAsync(new CommandDefinition(insertSql, new
            {
                ExpenseId = expenseId,
                d.Item,
                d.UnitPrice,
                d.Quantity,
                d.Amount,
                d.Description
            }, transaction: transaction));

            var id = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(lastIdSql, transaction: transaction));

            insertedIds.Add(id);
        }

        return insertedIds;
    }

    public async Task<IEnumerable<ExpenseDetail>> GetByExpenseIdAsync(Guid expenseId)
    {
        var transaction = _session.Transaction;
        var connection = transaction?.Connection ?? _session.Connection;

        const string sql = """
            SELECT 
                Id,
                ExpenseId,
                Item,
                UnitPrice,
                Quantity,
                Amount,
                Description
            FROM ExpenseDetail
            WHERE ExpenseId = @ExpenseId;
            """;
        return await connection.QueryAsync<ExpenseDetail>(new CommandDefinition(sql, new { ExpenseId = expenseId }, transaction: transaction));
    }

    public async Task DeleteByExpenseIdAsync(Guid expenseId)
    {
        var transaction = _session.Transaction;
        var connection = transaction?.Connection ?? _session.Connection;

        const string sql = "DELETE FROM ExpenseDetail WHERE ExpenseId = @ExpenseId;";
        await connection.ExecuteAsync(new CommandDefinition(sql, new { ExpenseId = expenseId }, transaction: transaction));
    }
}
