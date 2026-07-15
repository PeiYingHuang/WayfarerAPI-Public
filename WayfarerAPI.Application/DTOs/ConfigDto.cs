namespace WayfarerAPI.Application.DTOs;

/// <summary>
/// Config Currency DTO - 幣別設定資訊
/// </summary>
public sealed class ConfigCurrencyDto
{
    public string Code { get; set; } = string.Empty;
    public string? NumericCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public byte DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Config IATA DTO - 機場 IATA 代碼設定資訊
/// </summary>
public sealed class ConfigIataDto
{
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? Name { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
