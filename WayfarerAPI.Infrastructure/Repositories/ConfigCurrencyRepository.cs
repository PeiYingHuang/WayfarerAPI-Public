using Dapper;
using WayfarerAPI.Application.Interfaces.Data;
using WayfarerAPI.Application.Interfaces.Repositories;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Repositories;

public sealed class ConfigCurrencyRepository : IConfigCurrencyRepository
{
    private readonly IDbSession _session;

    public ConfigCurrencyRepository(IDbSession session)
    {
        _session = session;
    }

    public async Task<bool> ExistsByCodeAsync(string code)
    {
        const string sql = "SELECT 1 FROM ConfigCurrency WHERE Code = @Code LIMIT 1;";
        var exists = await _session.Connection.QuerySingleOrDefaultAsync<int?>(new CommandDefinition(sql, new { Code = code }));
        return exists.HasValue;
    }

    public async Task<IEnumerable<ConfigCurrency>> GetAllAsync()
    {
        const string sql = """
                           SELECT Code,
                                  NumericCode,
                                  Name,
                                  Symbol,
                                  DecimalPlaces,
                                  IsActive,
                                  CreatedAt
                           FROM ConfigCurrency
                           ORDER BY Code;
                           """;

        return await _session.Connection.QueryAsync<ConfigCurrency>(new CommandDefinition(sql));
    }

    public async Task UpsertAsync(List<ConfigCurrency> currencies)
    {
        if (currencies.Count == 0)
            return;

        const string sql = """
                           INSERT INTO ConfigCurrency (Code, NumericCode, Name, Symbol, IsActive)
                           VALUES (@Code, @NumericCode, @Name, @Symbol, @IsActive)
                           ON DUPLICATE KEY UPDATE
                               Name = VALUES(Name),
                               IsActive = VALUES(IsActive);
                           """;

        foreach (var currency in currencies)
        {
            await _session.Connection.ExecuteAsync(new CommandDefinition(
                sql,
                new
                {
                    currency.Code,
                    currency.NumericCode,
                    currency.Name,
                    currency.Symbol,
                    currency.IsActive
                },
                transaction: _session.Transaction));
        }
    }
}
