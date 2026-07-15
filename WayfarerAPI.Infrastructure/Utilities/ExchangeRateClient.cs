using System.Collections.Concurrent;
using System.Text.Json;
using WayfarerAPI.Application.Interfaces.Utilities;

namespace WayfarerAPI.Infrastructure.Utilities;

public sealed class ExchangeRateClient : IExchangeRateClient
{
    private static readonly HttpClient HttpClient = new();
    private static readonly ConcurrentDictionary<string, CacheItem> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    public async Task<decimal> GetRateAsync(string baseCurrencyCode, string targetCurrencyCode)
    {
        if (string.Equals(baseCurrencyCode, targetCurrencyCode, StringComparison.OrdinalIgnoreCase))
            return 1m;

        var rates = await GetRatesAsync(baseCurrencyCode);

        if (!rates.TryGetValue(targetCurrencyCode, out var rate))
            throw new InvalidOperationException($"找不到匯率：{baseCurrencyCode} -> {targetCurrencyCode}");

        return rate;
    }

    private static async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrencyCode)
    {
        if (Cache.TryGetValue(baseCurrencyCode, out var cached) && cached.ExpiresAtUtc > DateTime.UtcNow)
            return cached.Rates;

        var rates = await FetchRatesWithFallbackAsync(baseCurrencyCode);
        Cache[baseCurrencyCode] = new CacheItem
        {
            ExpiresAtUtc = DateTime.UtcNow.Add(CacheDuration),
            Rates = rates
        };

        return rates;
    }

    private static async Task<Dictionary<string, decimal>> FetchRatesWithFallbackAsync(string baseCurrencyCode)
    {
        var primaryUrl = $"https://api.exchangerate-api.com/v4/latest/{baseCurrencyCode}";
        var backupUrl = $"https://open.er-api.com/v6/latest/{baseCurrencyCode}";

        var primary = await TryFetchRatesAsync(primaryUrl);
        if (primary is not null)
            return primary;

        var backup = await TryFetchRatesAsync(backupUrl);
        if (backup is not null)
            return backup;

        throw new InvalidOperationException("匯率 API 呼叫失敗（主備援皆不可用）");
    }

    private static async Task<Dictionary<string, decimal>?> TryFetchRatesAsync(string url)
    {
        try
        {
            using var response = await HttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var json = await JsonDocument.ParseAsync(stream);

            if (!json.RootElement.TryGetProperty("rates", out var ratesElement) || ratesElement.ValueKind != JsonValueKind.Object)
                return null;

            var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in ratesElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var value))
                    rates[property.Name.ToUpperInvariant()] = value;
            }

            return rates.Count > 0 ? rates : null;
        }
        catch
        {
            return null;
        }
    }

    private sealed class CacheItem
    {
        public DateTime ExpiresAtUtc { get; set; }
        public Dictionary<string, decimal> Rates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
