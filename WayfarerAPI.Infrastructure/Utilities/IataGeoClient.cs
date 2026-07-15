using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WayfarerAPI.Application.Interfaces.Utilities;
using WayfarerAPI.Domain.Entities;

namespace WayfarerAPI.Infrastructure.Utilities;

public sealed class IataGeoClient : IIataGeoClient
{
    private static readonly HttpClient _httpClient = new();

    public async Task<ConfigIata?> GetAirportAsync(string iataCode)
    {
        var url = $"https://www.iatageo.com/v2/airports/iata/{Uri.EscapeDataString(iataCode)}";
        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<IataGeoResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result is not { Success: true, Data: not null })
            return null;

        return new ConfigIata
        {
            IataCode = result.Data.IataCode ?? iataCode.ToUpperInvariant(),
            IcaoCode = result.Data.IcaoCode,
            Name = result.Data.Name,
            Latitude = result.Data.Coordinates?.Latitude,
            Longitude = result.Data.Coordinates?.Longitude
        };
    }

    private sealed class IataGeoResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public IataGeoData? Data { get; set; }
    }

    private sealed class IataGeoData
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("iataCode")]
        public string? IataCode { get; set; }

        [JsonPropertyName("icaoCode")]
        public string? IcaoCode { get; set; }

        [JsonPropertyName("coordinates")]
        public IataGeoCoordinates? Coordinates { get; set; }
    }

    private sealed class IataGeoCoordinates
    {
        [JsonPropertyName("latitude")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public decimal? Longitude { get; set; }
    }
}
