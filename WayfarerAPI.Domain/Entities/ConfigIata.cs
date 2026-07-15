namespace WayfarerAPI.Domain.Entities;

public class ConfigIata
{
    public string IataCode { get; set; } = string.Empty;
    public string? IcaoCode { get; set; }
    public string? Name { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
}
