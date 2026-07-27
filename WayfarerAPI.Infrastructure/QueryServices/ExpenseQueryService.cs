using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.QueryServices;
using WayfarerAPI.Application.Models;

namespace WayfarerAPI.Infrastructure.QueryServices;

public class ExpenseQueryService : IExpenseQueryService
{
    private readonly IDbSession _session;

    public ExpenseQueryService(IDbSession session)
    {
        _session = session;
    }

    public async Task<IEnumerable<ExpenseInfoModel>> GetExpenseInfoByMemberIdAsync(Guid memberId)
    {
        const string sql = """
            SELECT e.ExpenseTime, e.Category, e.ItemName, es.SplitAmount
            FROM wayfarerdb.expensesplit AS es
                INNER JOIN expense AS e ON es.ExpenseId = e.id
            WHERE es.MemberId = @memberId
            ORDER BY e.ExpenseTime
            """;
        return await _session.Connection.QueryAsync<ExpenseInfoModel>(
            new CommandDefinition(sql, new { memberId }));
    }
}
