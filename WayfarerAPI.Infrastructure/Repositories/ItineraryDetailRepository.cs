using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class ItineraryDetailRepository : IItineraryDetailRepository
{
    private readonly IDbSession _session;

    public ItineraryDetailRepository(IDbSession session) => _session = session;

    public async Task<IEnumerable<ItineraryDetail>> GetByItineraryIdAsync(Guid itineraryId)
    {
        const string sql = """
            SELECT Id, ItineraryId, Title, Description, StartTime, EndTime,
                   LocationName, Latitude, Longitude, Category, SortOrder
            FROM ItineraryDetail
            WHERE ItineraryId = @ItineraryId
            ORDER BY SortOrder;
            """;

        return await _session.Connection.QueryAsync<ItineraryDetail>(new CommandDefinition(sql, new { ItineraryId = itineraryId }));
    }

    public async Task InsertAsync(ItineraryDetail detail)
    {
        const string sql = """
            INSERT INTO ItineraryDetail
                (Id, ItineraryId, Title, Description, StartTime, EndTime,
                 LocationName, Latitude, Longitude, Category, SortOrder)
            VALUES
                (@Id, @ItineraryId, @Title, @Description, @StartTime, @EndTime,
                 @LocationName, @Latitude, @Longitude, @Category, @SortOrder);
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            detail.Id,
            detail.ItineraryId,
            detail.Title,
            detail.Description,
            detail.StartTime,
            detail.EndTime,
            detail.LocationName,
            detail.Latitude,
            detail.Longitude,
            detail.Category,
            detail.SortOrder,
        }, transaction: _session.Transaction));
    }

    public async Task DeleteByItineraryIdAsync(Guid itineraryId)
    {
        const string sql = "DELETE FROM ItineraryDetail WHERE ItineraryId = @ItineraryId;";

        await _session.Connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { ItineraryId = itineraryId },
            transaction: _session.Transaction));
    }
}
