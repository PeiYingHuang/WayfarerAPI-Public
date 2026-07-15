using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;
using WayfarerAPI.Domain.Enumerations;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class TravellerCredentialRepository : ITravellerCredentialRepository
{
    private readonly IDbSession _session;

    public TravellerCredentialRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<TravellerCredential?> GetByProviderAsync(Guid travellerId, PasswordProviderEnum passwordProvider)
    {
        const string sql = "SELECT * FROM TravellerCredential WHERE TravellerId = @TravellerId AND Provider = @Provider LIMIT 1;";
        return await _session.Connection.QuerySingleOrDefaultAsync<TravellerCredential>(new CommandDefinition(sql, new { TravellerId = travellerId, Provider = passwordProvider.ToString() }));
    }

    public async Task<TravellerCredential?> GetByEmailAsync(string email)
    {
        const string sql = "SELECT * FROM TravellerCredential WHERE ProviderUserId = @Email AND Provider = 'Password' LIMIT 1;";
        return await _session.Connection.QuerySingleOrDefaultAsync<TravellerCredential>(new CommandDefinition(sql, new { Email = email, Provider = PasswordProviderEnum.Password.ToString() }));
    }

    public async Task InsertAsync(TravellerCredential credential)
    {
        const string sql = """
            INSERT INTO TravellerCredential (Id, TravellerId, Provider, ProviderUserId, PasswordHash, PasswordAlgo) 
            VALUES (@Id, @TravellerId, @Provider, @ProviderUserId, @PasswordHash, @PasswordAlgo);
            """;

        await _session.Connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                credential.Id,
                credential.TravellerId,
                credential.Provider,
                credential.ProviderUserId,
                credential.PasswordHash,
                credential.PasswordAlgo
            },
            transaction: _session.Transaction));
    }

    public async Task UpdateAsync(TravellerCredential credential)
    {
        const string sql = "UPDATE TravellerCredential SET PasswordHash = @PasswordHash, PasswordAlgo = @PasswordAlgo WHERE Id = @Id;";
        await _session.Connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                credential.Id,
                credential.PasswordHash,
                credential.PasswordAlgo
            },
            transaction: _session.Transaction));
    }
}
