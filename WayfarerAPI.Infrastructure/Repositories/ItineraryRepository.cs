using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class ItineraryRepository : IItineraryRepository
{
    private readonly IDbSession _session;

    public ItineraryRepository(IDbSession session) => _session = session;

    public async Task<Itinerary?> GetByTravelIdAndDateAsync(Guid travelId, DateTime date)
    {
        const string sql = """
            SELECT Id, TravelId, DayNumber, TravelDate, DayTitle
            FROM Itinerary
            WHERE TravelId = @TravelId AND DATE(TravelDate) = @TravelDate
            LIMIT 1;
            """;

        return await _session.Connection.QuerySingleOrDefaultAsync<Itinerary>(
            new CommandDefinition(
                sql,
                new { TravelId = travelId, TravelDate = date.Date },
                transaction: _session.Transaction));
    }

    public async Task<IEnumerable<Itinerary>> GetByTravelIdAsync(Guid travelId)
    {
        const string sql = """
            SELECT Id, TravelId, DayNumber, TravelDate, DayTitle
            FROM Itinerary
            WHERE TravelId = @TravelId
            ORDER BY TravelDate;
            """;

        return await _session.Connection.QueryAsync<Itinerary>(
            new CommandDefinition(
                sql,
                new { TravelId = travelId },
                transaction: _session.Transaction));
    }

    public async Task InsertAsync(Itinerary itinerary)
    {
        const string sql = """
            INSERT INTO Itinerary (Id, TravelId, DayNumber, TravelDate, DayTitle)
            VALUES (@Id, @TravelId, @DayNumber, @TravelDate, @DayTitle);
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            itinerary.Id,
            itinerary.TravelId,
            itinerary.DayNumber,
            itinerary.TravelDate,
            itinerary.DayTitle,
        }, transaction: _session.Transaction));
    }

    public async Task UpdateDayTitleAsync(Guid id, string? dayTitle)
    {
        const string sql = "UPDATE Itinerary SET DayTitle = @DayTitle WHERE Id = @Id;";

        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = id,
            DayTitle = dayTitle,
        }, transaction: _session.Transaction));
    }
}
