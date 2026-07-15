namespace WayfarerAPI.Domain.Entities;

public class Travel
{
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Destination { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CoverImageUrl { get; set; }
    public string ConsumptionCurrencyCode { get; set; } = "TWD";
    public string SettlementCurrencyCode { get; set; } = "TWD";
    public DateTime CreatedAt { get; set; }
}
