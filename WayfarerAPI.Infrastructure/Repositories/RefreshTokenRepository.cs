using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IWayfarerDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IWayfarerDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> CreateAsync(RefreshToken refreshToken)
    {
        const string sql = """
            INSERT INTO RefreshTokens (TravellerId, TokenHash, ExpiresAt, CreatedAt) 
            VALUES (@TravellerId, @TokenHash, @ExpiresAtUtc, @CreatedAtUtc); SELECT LAST_INSERT_ID();
            """; 
        
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            new
            {
                refreshToken.TravellerId,
                refreshToken.TokenHash,
                ExpiresAtUtc = refreshToken.ExpiresAt,
                CreatedAtUtc = refreshToken.CreatedAt
            }));
    }

    public async Task<RefreshToken?> GetValidByHashAsync(string tokenHash)
    {
        const string sql = """
                           SELECT Id AS Id,
                                  TravellerId,
                                  TokenHash,
                                  ExpiresAt,
                                  CreatedAt,
                                  RevokedAt
                           FROM RefreshTokens
                           WHERE TokenHash = @TokenHash
                             AND RevokedAt IS NULL
                             AND ExpiresAt > UTC_TIMESTAMP(6)
                           ORDER BY Id DESC
                           LIMIT 1;
                           """;

        using var connection = _connectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(new CommandDefinition(sql,new { TokenHash = tokenHash }));
    }

    public async Task RevokeAsync(long refreshTokenId)
    {
        const string sql = """
            UPDATE RefreshTokens 
            SET RevokedAt = UTC_TIMESTAMP(6) 
            WHERE Id = @Id AND RevokedAt IS NULL; 
            """;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(sql,new { Id = refreshTokenId }));
    }
}
