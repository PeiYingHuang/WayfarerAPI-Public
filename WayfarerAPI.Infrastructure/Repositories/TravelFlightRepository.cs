using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class TravelFlightRepository : ITravelFlightRepository
{
    private readonly IDbSession _session;

    public TravelFlightRepository(IDbSession session) => _session = session;

    public async Task<IEnumerable<TravelFlight>> GetByTravelIdAsync(Guid travelId)
    {
        const string sql = """
            SELECT Id, TravelId, FlightNumber, DepartureAirport, ArrivalAirport, DepartureAt, ArrivalAt, Direction, CreatedAt
            FROM TravelFlight WHERE TravelId = @TravelId ORDER BY DepartureAt;
            """;
        return await _session.Connection.QueryAsync<TravelFlight>(new CommandDefinition(sql, new { TravelId = travelId }));
    }

    public async Task InsertAsync(TravelFlight flight)
    {
        const string sql = """
            INSERT INTO TravelFlight (Id, TravelId, FlightNumber, DepartureAirport, ArrivalAirport, DepartureAt, ArrivalAt, Direction, CreatedAt)
            VALUES (@Id, @TravelId, @FlightNumber, @DepartureAirport, @ArrivalAirport, @DepartureAt, @ArrivalAt, @Direction, @CreatedAt);
            """;
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            flight.Id, flight.TravelId, flight.FlightNumber, flight.DepartureAirport,
            flight.ArrivalAirport, flight.DepartureAt, flight.ArrivalAt, flight.Direction, flight.CreatedAt
        }, transaction: _session.Transaction));
    }

    public async Task DeleteByTravelIdAsync(Guid travelId)
    {
        const string sql = "DELETE FROM TravelFlight WHERE TravelId = @TravelId;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { TravelId = travelId }, transaction: _session.Transaction));
    }
}
