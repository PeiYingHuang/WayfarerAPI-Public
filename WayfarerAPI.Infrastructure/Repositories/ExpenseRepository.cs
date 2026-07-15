using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly IDbSession _session;
    private readonly IExpenseDetailRepository _detailRepository;
    private readonly IExpenseSplitRepository _splitRepository;

    public ExpenseRepository(
        IDbSession session,
        IExpenseDetailRepository detailRepository,
        IExpenseSplitRepository splitRepository)
    {
        _session = session;
        _detailRepository = detailRepository;
        _splitRepository = splitRepository;
    }

    public async Task<Guid> InsertAsync(Expense expense)
    {
        if (expense.Id == Guid.Empty)
        {
            expense.Id = Guid.CreateVersion7();
        }

        const string sqlExpense = """
            INSERT INTO Expense
                (Id, TravelId, ItineraryDetailId, PayerMemberId, ItemName, ConsumptionAmount, CurrencyCode, ExchangeRate, SettlementAmount, Category, Note, ExpenseTime, CreatedBy)
            VALUES
                (@Id, @TravelId, @ItineraryDetailId, @PayerMemberId, @ItemName, @ConsumptionAmount, @CurrencyCode, @ExchangeRate, @SettlementAmount, @Category, @Note, @ExpenseTime, @CreatedBy);
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(sqlExpense, new
        {
            expense.Id,
            expense.TravelId,
            expense.ItineraryDetailId,
            expense.PayerMemberId,
            expense.ItemName,
            expense.ConsumptionAmount,
            expense.CurrencyCode,
            expense.ExchangeRate,
            expense.SettlementAmount,
            expense.Category,
            expense.Note,
            expense.ExpenseTime,
            expense.CreatedBy
        }, transaction: _session.Transaction));

        return expense.Id;
    }

    public async Task<IEnumerable<Expense>> GetByTravelIdAsync(Guid travelId)
    {
        const string sql = """
            SELECT 
                Id,
                TravelId,
                ItineraryDetailId,
                PayerMemberId,
                ItemName,
                ConsumptionAmount,
                CurrencyCode,
                ExchangeRate,
                SettlementAmount,
                Category,
                Note,
                ExpenseTime,
                CreatedBy,
                CreatedAt
            FROM Expense
            WHERE TravelId = @TravelId
            ORDER BY ExpenseTime DESC;
            """;
       return await _session.Connection.QueryAsync<Expense>(new CommandDefinition(sql, new { TravelId = travelId }));
    }

    public async Task<Expense?> GetByIdAsync(Guid expenseId)
    {
        const string sql = """
            SELECT 
                Id,
                TravelId,
                ItineraryDetailId,
                PayerMemberId,
                ItemName,
                ConsumptionAmount,
                CurrencyCode,
                ExchangeRate,
                SettlementAmount,
                Category,
                Note,
                ExpenseTime,
                CreatedBy,
                CreatedAt
            FROM Expense
            WHERE Id = @Id
            LIMIT 1;
            """;

        return await _session.Connection.QuerySingleOrDefaultAsync<Expense>(
            new CommandDefinition(sql, new { Id = expenseId }, transaction: _session.Transaction));
    }

    public async Task UpdateAsync(Guid expenseId, Guid payerMemberId, string? itemName, decimal consumptionAmount, string currencyCode, 
        decimal exchangeRate, decimal settlementAmount, string? category, string? note, DateTime? expenseTime)
    {
        const string sql = """
            UPDATE Expense
            SET PayerMemberId = @PayerMemberId,
                ConsumptionAmount = @ConsumptionAmount,
                CurrencyCode = @CurrencyCode,
                ExchangeRate = @ExchangeRate,
                SettlementAmount = @SettlementAmount,
                Category = COALESCE(NULLIF(@Category, ''), Category),
                Note = COALESCE(NULLIF(@Note, ''), Note),
                ExpenseTime = COALESCE(@ExpenseTime, ExpenseTime),
                ItemName = COALESCE(NULLIF(@ItemName, ''), ItemName)
            WHERE Id = @Id;
            """;
        await _session.Connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = expenseId, PayerMemberId = payerMemberId, ConsumptionAmount = consumptionAmount,
                CurrencyCode = currencyCode,
                ExchangeRate = exchangeRate, SettlementAmount = settlementAmount, 
                Category = category, Note = note, ExpenseTime = expenseTime, ItemName = itemName },
            transaction: _session.Transaction));
    }

    public async Task DeleteAsync(Guid travellerId, Guid expenseId)
    {
        const string sql = "DELETE FROM Expense WHERE Id = @Id AND CreatedBy = @CreatedBy;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { Id = expenseId, CreatedBy = travellerId }, transaction: _session.Transaction));
    }
}