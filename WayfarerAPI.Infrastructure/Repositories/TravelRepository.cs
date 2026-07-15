using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class TravelRepository : ITravelRepository
{
    private readonly IDbSession _session;

    public TravelRepository(IDbSession session) => _session = session;

    public async Task<Travel?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT Id,
                   CreatedBy,
                   Title,
                   Destination,
                   StartDate,
                   EndDate,
                   CoverImageUrl,
                   ConsumptionCurrencyCode,
                   SettlementCurrencyCode,
                   CreatedAt
            FROM Travel
            WHERE Id = @Id
            LIMIT 1;
            """;

        return await _session.Connection.QuerySingleOrDefaultAsync<Travel>(new CommandDefinition(sql, new { Id = id }));
    }

    public async Task<IEnumerable<Travel>> GetByTravellerIdAsync(Guid travellerId)
    {
        const string sql = """
            SELECT Id,
                   Title,
                   Destination,
                   StartDate,
                   EndDate,
                   CoverImageUrl,
                   ConsumptionCurrencyCode,
                   SettlementCurrencyCode,
                   CreatedBy,
                   CreatedAt
            FROM Travel
            WHERE CreatedBy = @TravellerId
            ORDER BY CreatedAt DESC;
            """;

        return await _session.Connection.QueryAsync<Travel>(new CommandDefinition(sql, new { TravellerId = travellerId }));
    }

    public async Task InsertAsync(Travel travel)
    {
        const string sql = """
            INSERT INTO Travel (Id, Title, Destination, StartDate, EndDate, CoverImageUrl, ConsumptionCurrencyCode, SettlementCurrencyCode, CreatedBy, CreatedAt)
            VALUES (@Id, @Title, @Destination, @StartDate, @EndDate, @CoverImageUrl, @ConsumptionCurrencyCode, @SettlementCurrencyCode, @CreatedBy, @CreatedAt);
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            travel.Id,
            travel.Title,
            travel.Destination,
            travel.StartDate,
            travel.EndDate,
            travel.CoverImageUrl,
            travel.ConsumptionCurrencyCode,
            travel.SettlementCurrencyCode,
            travel.CreatedBy,
            travel.CreatedAt
        }, transaction: _session.Transaction));
    }

    public async Task UpdateAsync(Travel travel)
    {
        const string sql = """
            UPDATE Travel
            SET Title = @Title,
                Destination = @Destination,
                StartDate = @StartDate,
                EndDate = @EndDate,
                ConsumptionCurrencyCode = @ConsumptionCurrencyCode,
                SettlementCurrencyCode = @SettlementCurrencyCode
            WHERE Id = @Id;
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            travel.Title,
            travel.Destination,
            travel.StartDate,
            travel.EndDate,
            travel.ConsumptionCurrencyCode,
            travel.SettlementCurrencyCode,
            travel.Id
        }, transaction: _session.Transaction));
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = "DELETE FROM Travel WHERE Id = @Id;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction));
    }
}
