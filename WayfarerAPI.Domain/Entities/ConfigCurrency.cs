namespace WayfarerAPI.Domain.Entities;

public class ConfigCurrency
{
    public string Code { get; set; } = string.Empty;
    public string? NumericCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public byte DecimalPlaces { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}