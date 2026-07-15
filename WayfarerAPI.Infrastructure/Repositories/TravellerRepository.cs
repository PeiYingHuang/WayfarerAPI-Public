using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class TravellerRepository : ITravellerRepository
{
    private readonly IDbSession _session;

    public TravellerRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<Traveller?> GetByIdAsync(Guid travellerId)
    {
        const string sql = "SELECT Id, Name FROM Traveller WHERE Id = @Id LIMIT 1;";
        return await _session.Connection.QuerySingleOrDefaultAsync<Traveller>(new CommandDefinition(sql, new { Id = travellerId }));
    }

    public async Task<IEnumerable<Traveller>> GetByIdsAsync(IList<Guid> travellerIds)
    {
        const string sql = "SELECT Id, Name FROM Traveller WHERE Id = ANY(@Ids);";
        return await _session.Connection.QueryAsync<Traveller>(new CommandDefinition(sql, new { Ids = travellerIds }));
    }

    public async Task InsertAsync(Traveller traveller)
    {
        const string sql = "INSERT INTO Traveller (Id, Name) VALUES (@Id, @Name);";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { traveller.Id, traveller.Name }, transaction: _session.Transaction));
    }

    public async Task UpdateAsync(Traveller traveller)
    {
        const string sql = "UPDATE Traveller SET Name = @Name WHERE Id = @Id;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { traveller.Id, traveller.Name }, transaction: _session.Transaction));
    }
}

