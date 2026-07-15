using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Utilities;

public sealed class FrankfurterClient : IFrankfurterClient
{
    private static readonly HttpClient _httpClient = new();

    public async Task<List<ConfigCurrency>> GetCurrenciesAsync()
    {
        const string url = "https://api.frankfurter.dev/v2/currencies";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return [];

        var payload = await response.Content.ReadFromJsonAsync<List<FrankfurterCurrencyItem>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (payload is null || payload.Count == 0)
            return [];

        return payload
            .Where(x => !string.IsNullOrWhiteSpace(x.IsoCode) && !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => new ConfigCurrency
            {
                Code = x.IsoCode!.Trim().ToUpperInvariant(),
                NumericCode = string.IsNullOrWhiteSpace(x.IsoNumeric) ? null : x.IsoNumeric.Trim(),
                Name = x.Name!.Trim(),
                Symbol = string.IsNullOrWhiteSpace(x.Symbol) ? null : x.Symbol.Trim(),
                DecimalPlaces = 2,
                IsActive = true
            })
            .ToList();
    }

    private sealed class FrankfurterCurrencyItem
    {
        [JsonPropertyName("iso_code")]
        public string? IsoCode { get; set; }

        [JsonPropertyName("iso_numeric")]
        public string? IsoNumeric { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }
    }
}