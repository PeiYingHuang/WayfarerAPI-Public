namespace WayfarerAPI.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; }
    public Guid TravelId { get; set; }
    public Guid? ItineraryDetailId { get; set; }
    public Guid PayerMemberId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal ConsumptionAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal SettlementAmount { get; set; }
    public string? Category { get; set; }
    public string? Note { get; set; }
    public DateTime ExpenseTime { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}