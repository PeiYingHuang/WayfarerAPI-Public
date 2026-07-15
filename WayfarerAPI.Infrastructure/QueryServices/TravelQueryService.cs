using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.QueryServices;
using WayfarerAPI.Application.Models;

namespace WayfarerAPI.Infrastructure.QueryServices;

public class TravelQueryService : ITravelQueryService
{
    private readonly IDbSession _session;

    public TravelQueryService(IDbSession session)
    {
        _session = session;
    }

    public async Task<TravellerInfoModel?> GetTravellerInfoByTravellerIdAsync(Guid travellerId)
    {
        const string sql = """
            SELECT t.Id, t.Name, tc.ProviderUserId AS Email
            FROM Traveller AS t
                INNER JOIN TravellerCredential AS tc ON t.Id = tc.TravellerId AND tc.Provider = 'Password'
            WHERE t.Id = @travellerId
            """;
        return await _session.Connection.QueryFirstOrDefaultAsync<TravellerInfoModel>(
            new CommandDefinition(sql, new { travellerId }));
    }

    public async Task<TravellerInfoModel?> GetTravellerInfoByEmailAsync(string email)
    {
        const string sql = """
            SELECT t.Id, t.Name, tc.ProviderUserId AS Email
            FROM Traveller AS t
                INNER JOIN TravellerCredential AS tc ON t.Id = tc.TravellerId AND tc.Provider = 'Password'
            WHERE tc.ProviderUserId = @Email
            """;
        return await _session.Connection.QueryFirstOrDefaultAsync<TravellerInfoModel>(
            new CommandDefinition(sql, new { Email = email }));
    }

    public async Task<IEnumerable<TravellerInfoModel>> GetFriendInfoByTravellerIdAsync(Guid travellerId)
    {
        const string sql = """
            WITH Travel AS (
            SELECT TravelId
            FROM travelmember
            WHERE TravellerId = @travellerId)

            SELECT t.Id, t.Name, tc.ProviderUserId AS Email
            FROM TravelMember AS tm
            	INNER JOIN Traveller AS t ON tm.TravellerId = t.Id
                INNER JOIN TravellerCredential AS tc ON t.Id = tc.TravellerId AND tc.Provider = 'Password'
            WHERE tm.TravelId IN (SELECT TravelId FROM Travel) AND tm.TravellerId != @travellerId
            """;
        return await _session.Connection.QueryAsync<TravellerInfoModel>(
            new CommandDefinition(sql, new { travellerId }));
    }

    public async Task<IEnumerable<TravelSummaryModel>> GetAllSummaryByTravellerIdAsync(Guid travellerId)
    {
        const string sql = """
            SELECT 
                t.Id,
                t.Title,
                t.Destination,
                t.StartDate,
                t.EndDate,
                t.CreatedAt,
                COALESCE(SUM(CASE WHEN m.MemberType = 'Adult' THEN 1 ELSE 0 END), 0) AS AdultCount,
                COALESCE(SUM(CASE WHEN m.MemberType = 'Child' THEN 1 ELSE 0 END), 0) AS ChildCount,
                COALESCE((SELECT COUNT(*) FROM TravelFlight WHERE TravelId = t.Id), 0) AS FlightCount
            FROM Travel t
                LEFT JOIN TravelMember m ON t.Id = m.TravelId
            WHERE t.CreatedBy = @TravellerId OR m.TravellerId = @TravellerId
            GROUP BY t.Id, t.Title, t.Destination, t.StartDate, t.EndDate, t.CreatedAt
            ORDER BY t.CreatedAt DESC;
            """;

        return await _session.Connection.QueryAsync<TravelSummaryModel>(
            new CommandDefinition(sql, new { TravellerId = travellerId }));
    }
}
