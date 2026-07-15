using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class ConfigIataRepository : IConfigIataRepository
{
    private readonly IDbSession _session;

    public ConfigIataRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<ConfigIata?> GetByIataCodeAsync(string iataCode)
    {
        const string sql = "SELECT IataCode, IcaoCode, Name, Latitude, Longitude FROM ConfigIATA WHERE IataCode = @IataCode LIMIT 1;";
        return await _session.Connection.QuerySingleOrDefaultAsync<ConfigIata>(new CommandDefinition(sql, new { IataCode = iataCode }));
    }

    public async Task InsertAsync(ConfigIata entity)
    {
        const string sql = "INSERT INTO ConfigIATA (IataCode, IcaoCode, Name, Latitude, Longitude) VALUES (@IataCode, @IcaoCode, @Name, @Latitude, @Longitude);";
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { entity.IataCode, entity.IcaoCode, entity.Name, entity.Latitude, entity.Longitude }, transaction: _session.Transaction));
    }
}
